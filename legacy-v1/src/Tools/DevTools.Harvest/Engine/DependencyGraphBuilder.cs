using System.Text.RegularExpressions;
using DevTools.Harvest.Configuration;
using DevTools.Harvest.Models;

namespace DevTools.Harvest.Engine;

internal sealed class DependencyGraphBuilder
{
    private static readonly Regex UsingRegex = new(
        @"^\s*using\s+(?:static\s+)?(?:\w+\s*=\s*)?([A-Za-z0-9_.]+)\s*;",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex NewTypeRegex = new(
        @"\bnew\s+([A-Za-z_][A-Za-z0-9_]*)\b",
        RegexOptions.Compiled);

    private static readonly Regex StaticAccessRegex = new(
        @"\b([A-Za-z_][A-Za-z0-9_]*)\s*\.",
        RegexOptions.Compiled);

    private static readonly Regex GenericTypeRegex = new(
        @"\b([A-Za-z_][A-Za-z0-9_]*)\s*<",
        RegexOptions.Compiled);

    private static readonly Regex BaseTypeRegex = new(
        @":\s*([A-Za-z_][A-Za-z0-9_]*)",
        RegexOptions.Compiled);

    private static readonly Regex JsImportRegex = new(
        @"^\s*import\s+.*?\s+from\s+['""]([^'""]+)['""]",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex JsSideEffectImportRegex = new(
        @"^\s*import\s+['""]([^'""]+)['""]",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex JsRequireRegex = new(
        @"\brequire\(\s*['""]([^'""]+)['""]\s*\)",
        RegexOptions.Compiled);

    public DependencyGraph Build(IReadOnlyList<FileNode> files, HarvestRules rules, string rootPath, CancellationToken ct)
    {
        var fanIn = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var fanOut = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        var namespaceMap = new Dictionary<string, List<FileNode>>(StringComparer.OrdinalIgnoreCase);
        var typeMap = new Dictionary<string, List<FileNode>>(StringComparer.Ordinal);

        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();

            fanIn[file.FullPath] = 0;
            fanOut[file.FullPath] = 0;

            if (file.Extension.Equals(".cs", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(file.Namespace))
                {
                    if (!namespaceMap.TryGetValue(file.Namespace, out var list))
                    {
                        list = new List<FileNode>();
                        namespaceMap[file.Namespace] = list;
                    }

                    list.Add(file);
                }

                foreach (var typeName in file.DeclaredTypes)
                {
                    if (!typeMap.TryGetValue(typeName, out var list))
                    {
                        list = new List<FileNode>();
                        typeMap[typeName] = list;
                    }

                    list.Add(file);
                }
            }
        }

        var allowedExtensions = new HashSet<string>(rules.Extensions, StringComparer.OrdinalIgnoreCase);
        var fileByFullPath = files.ToDictionary(f => f.FullPath, StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();

            var outgoing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (file.Extension.Equals(".cs", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var ns in ExtractUsings(file.Content, rules.IgnoreUsingPrefixes))
                {
                    if (namespaceMap.TryGetValue(ns, out var targets))
                        AddTargets(outgoing, targets);
                    else
                        AddNamespacePrefixMatches(outgoing, namespaceMap, ns);
                }

                var identifiers = ExtractTypeUsageTokens(file.Content);
                foreach (var identifier in identifiers)
                {
                    if (typeMap.TryGetValue(identifier, out var targets))
                        AddTargets(outgoing, targets);
                }
            }
            else if (IsJsOrTs(file.Extension))
            {
                foreach (var specifier in ExtractJsImports(file.Content))
                {
                    var target = ResolveJsTsImport(specifier, file.FullPath, rootPath, allowedExtensions, fileByFullPath);
                    if (target is not null)
                        outgoing.Add(target.FullPath);
                }
            }

            outgoing.Remove(file.FullPath);

            fanOut[file.FullPath] = outgoing.Count;

            foreach (var target in outgoing)
            {
                if (fanIn.ContainsKey(target))
                    fanIn[target] += 1;
            }
        }

        return new DependencyGraph(fanIn, fanOut);
    }

    private static bool IsJsOrTs(string extension)
        => extension.Equals(".js", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".ts", StringComparison.OrdinalIgnoreCase);

    private static IEnumerable<string> ExtractUsings(string content, IReadOnlyList<string> ignoredPrefixes)
    {
        foreach (Match match in UsingRegex.Matches(content))
        {
            if (match.Groups.Count < 2) continue;
            var value = match.Groups[1].Value.Trim();
            if (!string.IsNullOrWhiteSpace(value) && !IsIgnoredUsing(value, ignoredPrefixes))
                yield return value;
        }
    }

    private static HashSet<string> ExtractTypeUsageTokens(string content)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        CollectMatches(set, NewTypeRegex.Matches(content));
        CollectMatches(set, StaticAccessRegex.Matches(content));
        CollectMatches(set, GenericTypeRegex.Matches(content));
        CollectMatches(set, BaseTypeRegex.Matches(content));
        return set;
    }

    private static void CollectMatches(HashSet<string> set, MatchCollection matches)
    {
        foreach (Match match in matches)
        {
            if (!match.Success) continue;
            if (match.Groups.Count < 2) continue;
            var value = match.Groups[1].Value.Trim();
            if (!string.IsNullOrWhiteSpace(value))
                set.Add(value);
        }
    }

    private static bool IsIgnoredUsing(string ns, IReadOnlyList<string> ignoredPrefixes)
    {
        if (ignoredPrefixes.Count == 0)
            return false;

        foreach (var prefix in ignoredPrefixes)
        {
            if (string.IsNullOrWhiteSpace(prefix)) continue;
            if (ns.Equals(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
            if (ns.StartsWith(prefix + ".", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static void AddNamespacePrefixMatches(
        HashSet<string> outgoing,
        IDictionary<string, List<FileNode>> namespaceMap,
        string ns)
    {
        foreach (var kvp in namespaceMap)
        {
            if (!kvp.Key.StartsWith(ns + ".", StringComparison.OrdinalIgnoreCase))
                continue;

            AddTargets(outgoing, kvp.Value);
        }
    }

    private static void AddTargets(HashSet<string> outgoing, IEnumerable<FileNode> targets)
    {
        foreach (var target in targets)
            outgoing.Add(target.FullPath);
    }

    private static IEnumerable<string> ExtractJsImports(string content)
    {
        foreach (Match match in JsImportRegex.Matches(content))
        {
            if (match.Groups.Count < 2) continue;
            var value = match.Groups[1].Value.Trim();
            if (!string.IsNullOrWhiteSpace(value))
                yield return value;
        }

        foreach (Match match in JsSideEffectImportRegex.Matches(content))
        {
            if (match.Groups.Count < 2) continue;
            var value = match.Groups[1].Value.Trim();
            if (!string.IsNullOrWhiteSpace(value))
                yield return value;
        }

        foreach (Match match in JsRequireRegex.Matches(content))
        {
            if (match.Groups.Count < 2) continue;
            var value = match.Groups[1].Value.Trim();
            if (!string.IsNullOrWhiteSpace(value))
                yield return value;
        }
    }

    private static FileNode? ResolveJsTsImport(
        string specifier,
        string sourceFullPath,
        string root,
        IReadOnlySet<string> allowedExtensions,
        IReadOnlyDictionary<string, FileNode> fileByFullPath)
    {
        if (string.IsNullOrWhiteSpace(specifier))
            return null;

        var isRelative = specifier.StartsWith(".", StringComparison.OrdinalIgnoreCase);
        var isRooted = specifier.StartsWith("/", StringComparison.OrdinalIgnoreCase);
        if (!isRelative && !isRooted)
            return null;

        var basePath = isRooted
            ? Path.GetFullPath(Path.Combine(root, specifier.TrimStart('/')))
            : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourceFullPath) ?? root, specifier));

        var candidates = new List<string>();

        if (Path.HasExtension(basePath))
        {
            candidates.Add(basePath);
        }
        else
        {
            foreach (var ext in allowedExtensions)
                candidates.Add(basePath + ext);

            foreach (var ext in allowedExtensions)
                candidates.Add(Path.Combine(basePath, "index" + ext));
        }

        foreach (var candidate in candidates)
        {
            if (fileByFullPath.TryGetValue(candidate, out var file))
                return file;
        }

        return null;
    }

    internal sealed record DependencyGraph(
        IReadOnlyDictionary<string, int> FanIn,
        IReadOnlyDictionary<string, int> FanOut)
    {
        public int GetFanIn(string fullPath)
            => FanIn.TryGetValue(fullPath, out var value) ? value : 0;

        public int GetFanOut(string fullPath)
            => FanOut.TryGetValue(fullPath, out var value) ? value : 0;
    }
}
