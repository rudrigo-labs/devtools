using System.Text;
using System.Text.RegularExpressions;
using DevTools.Core.Abstractions;
using DevTools.Core.Models;
using DevTools.Core.Results;
using DevTools.Harvest.Configuration;
using DevTools.Harvest.Models;
using DevTools.Core.Providers;
using DevTools.Harvest.Validation;

namespace DevTools.Harvest.Engine;

public sealed class HarvestEngine : IDevToolEngine<HarvestRequest, HarvestResponse>
{
    private static readonly Regex NamespaceRegex = new(
        @"^\s*namespace\s+([A-Za-z0-9_.]+)\s*(;|{)",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex TypeRegex = new(
        @"\b(?:class|interface|struct|record|enum)\s+([A-Za-z_][A-Za-z0-9_]*)",
        RegexOptions.Compiled);

    private static readonly Regex PublicStaticRegex = new(
        @"\bpublic\s+static\b",
        RegexOptions.Compiled);

    private static readonly Regex MainMethodRegex = new(
        @"\bstatic\s+(?:async\s+)?(?:Task|void|int)\s+Main\s*\(",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly IFileSystem _fs;
    private readonly DependencyGraphBuilder _graphBuilder = new();
    private readonly ScoringEngine _scoring = new();

    public HarvestEngine(IFileSystem? fileSystem = null)
    {
        _fs = fileSystem ?? new SystemFileSystem();
    }

    public async Task<RunResult<HarvestResponse>> ExecuteAsync(
        HarvestRequest request,
        IProgressReporter? progress = null,
        CancellationToken ct = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var errors = HarvestRequestValidator.Validate(request, _fs);
        if (errors.Count > 0)
            return RunResult<HarvestResponse>.Fail(errors);

        progress?.Report(new ProgressEvent("Loading config", 0, "config"));

        var configResult = await HarvestConfigLoader.LoadAsync(_fs, request.ConfigPath, ct).ConfigureAwait(false);
        if (!configResult.IsSuccess || configResult.Value is null)
            return RunResult<HarvestResponse>.Fail(configResult.Errors);

        var config = configResult.Value;
        var minScore = request.MinScore ?? config.MinScoreDefault;

        var issues = new List<ErrorDetail>();

        progress?.Report(new ProgressEvent("Enumerating files", 5, "scan"));

        var filePaths = EnumerateFiles(request.RootPath, config.Rules, ct).ToList();
        var totalFiles = filePaths.Count;

        progress?.Report(new ProgressEvent("Reading files", 15, "scan"));

        var nodes = BuildFileNodes(request.RootPath, filePaths, config.Rules, issues, progress, ct);

        progress?.Report(new ProgressEvent("Building dependency graph", 50, "graph"));

        var graph = _graphBuilder.Build(nodes, config.Rules, request.RootPath, ct);

        progress?.Report(new ProgressEvent("Scoring", 70, "score"));

        var hits = new List<HarvestHit>();
        foreach (var file in nodes)
        {
            ct.ThrowIfCancellationRequested();

            var densities = KeywordAnalyzer.Analyze(
                file.Content,
                file.LineCount,
                config.Categories,
                config.Weights.DensityScale);

            var hit = _scoring.Score(file, graph, config, densities);
            if (hit.Score >= minScore)
                hits.Add(hit);
        }

        var ordered = hits
            .OrderByDescending(h => h.Score)
            .ThenBy(h => h.File, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var initialIssuesCount = issues.Count;
        if (request.CopyFiles && !string.IsNullOrWhiteSpace(request.OutputPath))
        {
            progress?.Report(new ProgressEvent("Copying files", 90, "copy"));
            CopyFiles(ordered, request.RootPath, request.OutputPath, issues);
        }
        var copyErrors = issues.Count - initialIssuesCount;
        var changed = (request.CopyFiles && !string.IsNullOrWhiteSpace(request.OutputPath)) 
            ? ordered.Count - copyErrors 
            : 0;

        progress?.Report(new ProgressEvent("Done", 100, "done"));
        sw.Stop();

        var report = new HarvestReport(
            request.RootPath,
            nodes.Count,
            hits.Count,
            ordered,
            issues);

        var summary = new RunSummary(
            ToolName: "Harvest",
            Mode: request.CopyFiles ? "Real" : "DryRun",
            MainInput: request.RootPath,
            OutputLocation: request.OutputPath,
            Processed: totalFiles,
            Changed: changed,
            Ignored: totalFiles - hits.Count, // Ignored = Total - Hits (approx)
            Failed: issues.Count,
            Duration: sw.Elapsed
        );

        return RunResult<HarvestResponse>.Success(new HarvestResponse(report)).WithSummary(summary);
    }

    private List<FileNode> BuildFileNodes(
        string rootPath,
        IReadOnlyList<string> filePaths,
        HarvestRules rules,
        List<ErrorDetail> issues,
        IProgressReporter? progress,
        CancellationToken ct)
    {
        var nodes = new List<FileNode>(filePaths.Count);
        var total = filePaths.Count;
        var idx = 0;

        foreach (var fullPath in filePaths)
        {
            ct.ThrowIfCancellationRequested();

            idx++;
            if (idx % 100 == 0)
                progress?.Report(new ProgressEvent($"Processing {idx}/{total}", (int)(idx * 100.0 / Math.Max(1, total)), "scan"));

            if (rules.MaxFileSizeKb.HasValue)
            {
                try
                {
                    var info = new FileInfo(fullPath);
                    if (info.Length > rules.MaxFileSizeKb.Value * 1024L)
                        continue;
                }
                catch (Exception ex)
                {
                    issues.Add(new ErrorDetail("harvest.file.size_error", "Failed to read file size.", Cause: fullPath, Exception: ex));
                    continue;
                }
            }

            try
            {
                if (!TryReadText(fullPath, out var content))
                    continue;
                var extension = Path.GetExtension(fullPath);
                var relative = Path.GetRelativePath(rootPath, fullPath).Replace('\\', '/');
                var lineCount = CountEffectiveLines(content);

                string? ns = null;
                if (extension.Equals(".cs", StringComparison.OrdinalIgnoreCase))
                    ns = ExtractNamespace(content);

                var types = extension.Equals(".cs", StringComparison.OrdinalIgnoreCase)
                    ? ExtractTypeNames(content)
                    : Array.Empty<string>();

                var publicStaticCount = extension.Equals(".cs", StringComparison.OrdinalIgnoreCase)
                    ? CountPublicStatic(content)
                    : 0;

                var isEntrypoint = extension.Equals(".cs", StringComparison.OrdinalIgnoreCase)
                    && IsEntrypointCandidate(fullPath, content);

                nodes.Add(new FileNode(
                    fullPath,
                    relative,
                    extension,
                    content,
                    lineCount,
                    ns,
                    types,
                    publicStaticCount,
                    isEntrypoint));
            }
            catch (Exception ex)
            {
                issues.Add(new ErrorDetail("harvest.file.read_error", "Failed to read file.", Cause: fullPath, Exception: ex));
            }
        }

        return nodes;
    }

    private bool TryReadText(string path, out string content)
    {
        content = string.Empty;

        var bytes = _fs.ReadAllBytes(path);
        var detection = DetectText(bytes);
        if (detection.IsBinary)
            return false;

        content = detection.Content;
        return true;
    }

    private IEnumerable<string> EnumerateFiles(string rootPath, HarvestRules rules, CancellationToken ct)
    {
        var allowedExtensions = new HashSet<string>(rules.Extensions, StringComparer.OrdinalIgnoreCase);
        var excluded = new HashSet<string>(rules.ExcludeDirectories, StringComparer.OrdinalIgnoreCase);

        var stack = new Stack<string>();
        stack.Push(rootPath);

        while (stack.Count > 0)
        {
            ct.ThrowIfCancellationRequested();

            var dir = stack.Pop();

            IEnumerable<string> subDirs = Array.Empty<string>();
            IEnumerable<string> files = Array.Empty<string>();

            try
            {
                subDirs = _fs.EnumerateDirectories(dir, "*", SearchOption.TopDirectoryOnly);
                files = _fs.EnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly);
            }
            catch
            {
                continue;
            }

            foreach (var subDir in subDirs)
            {
                var name = Path.GetFileName(subDir);
                if (excluded.Contains(name))
                    continue;

                stack.Push(subDir);
            }

            foreach (var file in files)
            {
                var ext = Path.GetExtension(file);
                if (allowedExtensions.Count > 0 && !allowedExtensions.Contains(ext))
                    continue;

                yield return file;
            }
        }
    }

    private static int CountEffectiveLines(string content)
    {
        if (string.IsNullOrEmpty(content))
            return 0;

        var count = 0;
        using var reader = new StringReader(content);
        string? line;
        var inBlockComment = false;

        while ((line = reader.ReadLine()) is not null)
        {
            var trimmed = line.Trim();

            if (string.IsNullOrWhiteSpace(trimmed))
                continue;

            if (inBlockComment)
            {
                var endIdx = trimmed.IndexOf("*/", StringComparison.Ordinal);
                if (endIdx >= 0)
                {
                    inBlockComment = false;
                    trimmed = trimmed[(endIdx + 2)..].Trim();
                    if (string.IsNullOrWhiteSpace(trimmed))
                        continue;
                }
                else
                {
                    continue;
                }
            }

            if (trimmed.StartsWith("//", StringComparison.Ordinal))
                continue;

            var startIdx = trimmed.IndexOf("/*", StringComparison.Ordinal);
            if (startIdx >= 0)
            {
                var endIdx = trimmed.IndexOf("*/", startIdx + 2, StringComparison.Ordinal);
                if (endIdx >= 0)
                {
                    trimmed = (trimmed[..startIdx] + trimmed[(endIdx + 2)..]).Trim();
                    if (string.IsNullOrWhiteSpace(trimmed))
                        continue;
                }
                else
                {
                    inBlockComment = true;
                    trimmed = trimmed[..startIdx].Trim();
                    if (string.IsNullOrWhiteSpace(trimmed))
                        continue;
                }
            }

            count++;
        }

        return count;
    }

    private static DetectedText DetectText(byte[] bytes)
    {
        if (bytes.Length == 0)
            return new DetectedText(string.Empty, false);

        var bom = GetBomEncoding(bytes, out var bomLength);
        if (bom is not null)
        {
            var content = bom.GetString(bytes, bomLength, bytes.Length - bomLength);
            return new DetectedText(content, false);
        }

        if (TryDetectUtf32(bytes, out var utf32Encoding))
        {
            var content = utf32Encoding.GetString(bytes);
            return new DetectedText(content, false);
        }

        if (TryDetectUtf16(bytes, out var utf16Encoding))
        {
            var content = utf16Encoding.GetString(bytes);
            return new DetectedText(content, false);
        }

        if (TryDecodeUtf8(bytes, out var utf8))
            return new DetectedText(utf8, false);

        if (LooksBinary(bytes))
            return new DetectedText(string.Empty, true);

        var fallback = Encoding.GetEncoding(1252);
        var fallbackText = fallback.GetString(bytes);
        return new DetectedText(fallbackText, false);
    }

    private static Encoding? GetBomEncoding(byte[] bytes, out int bomLength)
    {
        bomLength = 0;
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            bomLength = 3;
            return new UTF8Encoding(true, true);
        }

        if (bytes.Length >= 4 && bytes[0] == 0xFF && bytes[1] == 0xFE && bytes[2] == 0x00 && bytes[3] == 0x00)
        {
            bomLength = 4;
            return new UTF32Encoding(false, true);
        }

        if (bytes.Length >= 4 && bytes[0] == 0x00 && bytes[1] == 0x00 && bytes[2] == 0xFE && bytes[3] == 0xFF)
        {
            bomLength = 4;
            return new UTF32Encoding(true, true);
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        {
            bomLength = 2;
            return new UnicodeEncoding(false, true);
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
        {
            bomLength = 2;
            return new UnicodeEncoding(true, true);
        }

        return null;
    }

    private static bool TryDecodeUtf8(byte[] bytes, out string content)
    {
        try
        {
            var utf8 = new UTF8Encoding(false, true);
            content = utf8.GetString(bytes);
            return true;
        }
        catch
        {
            content = string.Empty;
            return false;
        }
    }

    private static bool TryDetectUtf16(byte[] bytes, out Encoding encoding)
    {
        encoding = new UnicodeEncoding(false, false);

        if (!bytes.Any(b => b == 0))
            return false;

        var sample = bytes.Length > 4096 ? bytes.AsSpan(0, 4096) : bytes.AsSpan();
        int evenZero = 0, oddZero = 0;
        int evenCount = 0, oddCount = 0;

        for (int i = 0; i < sample.Length; i++)
        {
            if (i % 2 == 0)
            {
                evenCount++;
                if (sample[i] == 0) evenZero++;
            }
            else
            {
                oddCount++;
                if (sample[i] == 0) oddZero++;
            }
        }

        if (evenCount == 0 || oddCount == 0)
            return false;

        var evenRatio = evenZero / (double)evenCount;
        var oddRatio = oddZero / (double)oddCount;

        if (oddRatio > 0.6 && evenRatio < 0.2)
        {
            encoding = new UnicodeEncoding(false, false);
            return true;
        }

        if (evenRatio > 0.6 && oddRatio < 0.2)
        {
            encoding = new UnicodeEncoding(true, false);
            return true;
        }

        return false;
    }

    private static bool TryDetectUtf32(byte[] bytes, out Encoding encoding)
    {
        encoding = new UTF32Encoding(false, false);

        if (bytes.Length < 4 || !bytes.Any(b => b == 0))
            return false;

        var sample = bytes.Length > 4096 ? bytes.AsSpan(0, 4096) : bytes.AsSpan();
        int mod0 = 0, mod1 = 0, mod2 = 0, mod3 = 0;
        int mod0Count = 0, mod1Count = 0, mod2Count = 0, mod3Count = 0;

        for (int i = 0; i < sample.Length; i++)
        {
            var mod = i % 4;
            switch (mod)
            {
                case 0:
                    mod0Count++;
                    if (sample[i] == 0) mod0++;
                    break;
                case 1:
                    mod1Count++;
                    if (sample[i] == 0) mod1++;
                    break;
                case 2:
                    mod2Count++;
                    if (sample[i] == 0) mod2++;
                    break;
                case 3:
                    mod3Count++;
                    if (sample[i] == 0) mod3++;
                    break;
            }
        }

        if (mod0Count == 0 || mod1Count == 0 || mod2Count == 0 || mod3Count == 0)
            return false;

        var non0ZeroRatio = (mod1 + mod2 + mod3) / (double)(mod1Count + mod2Count + mod3Count);
        var mod0Ratio = mod0 / (double)mod0Count;

        if (non0ZeroRatio > 0.7 && mod0Ratio < 0.2)
        {
            encoding = new UTF32Encoding(false, false);
            return true;
        }

        var mod3Ratio = mod3 / (double)mod3Count;
        var non3ZeroRatio = (mod0 + mod1 + mod2) / (double)(mod0Count + mod1Count + mod2Count);

        if (non3ZeroRatio > 0.7 && mod3Ratio < 0.2)
        {
            encoding = new UTF32Encoding(true, false);
            return true;
        }

        return false;
    }

    private static bool LooksBinary(byte[] bytes)
    {
        var sample = bytes.Length > 4096 ? bytes.AsSpan(0, 4096) : bytes.AsSpan();
        if (sample.Length == 0) return false;

        var controlCount = 0;
        foreach (var b in sample)
        {
            if (b == 0) return true;

            if (b < 9 || (b > 13 && b < 32))
                controlCount++;
        }

        var ratio = controlCount / (double)sample.Length;
        return ratio > 0.3;
    }

    private readonly record struct DetectedText(string Content, bool IsBinary);

    private static string? ExtractNamespace(string content)
    {
        var match = NamespaceRegex.Match(content);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    private static IReadOnlyList<string> ExtractTypeNames(string content)
    {
        var list = new List<string>();
        foreach (Match match in TypeRegex.Matches(content))
        {
            if (match.Groups.Count < 2) continue;
            var value = match.Groups[1].Value.Trim();
            if (!string.IsNullOrWhiteSpace(value))
                list.Add(value);
        }

        return list;
    }

    private static int CountPublicStatic(string content)
    {
        return PublicStaticRegex.Matches(content).Count;
    }

    private static bool IsEntrypointCandidate(string fullPath, string content)
    {
        var fileName = Path.GetFileName(fullPath);
        if (fileName.Equals("Program.cs", StringComparison.OrdinalIgnoreCase))
            return true;

        return MainMethodRegex.IsMatch(content);
    }

    private void CopyFiles(List<HarvestHit> hits, string rootPath, string outputPath, List<ErrorDetail> issues)
    {
        if (!_fs.DirectoryExists(outputPath))
        {
            try
            {
                _fs.CreateDirectory(outputPath);
            }
            catch (Exception ex)
            {
                issues.Add(new ErrorDetail("harvest.copy.create_dir_error", "Failed to create output directory.", Cause: outputPath, Action: "Check permissions or path validity", Exception: ex));
                return;
            }
        }

        foreach (var hit in hits)
        {
            try
            {
                var sourceFile = hit.File;
                // Preserve relative path structure
                var relativePath = Path.GetRelativePath(rootPath, sourceFile);
                var destFile = Path.Combine(outputPath, relativePath);
                var destDir = Path.GetDirectoryName(destFile);

                if (destDir is not null && !_fs.DirectoryExists(destDir))
                    _fs.CreateDirectory(destDir);

                _fs.CopyFile(sourceFile, destFile, overwrite: true);
            }
            catch (Exception ex)
            {
                issues.Add(new ErrorDetail("harvest.copy.file_error", "Failed to copy file.", Cause: hit.File, Action: "Check file access or disk space", Exception: ex));
            }
        }
    }
}
