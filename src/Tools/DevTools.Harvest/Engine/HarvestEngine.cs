using System.Text;
using System.Text.RegularExpressions;
using DevTools.Core.Abstractions;
using DevTools.Core.Models;
using DevTools.Core.Results;
using DevTools.Core.Validation;
using DevTools.Harvest.Models;
using DevTools.Harvest.Validators;

namespace DevTools.Harvest.Engine;

public sealed class HarvestEngine : IDevToolEngine<HarvestRequest, HarvestResult>
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

    private readonly IValidator<HarvestRequest> _validator;
    private readonly DependencyGraphBuilder _graphBuilder = new();
    private readonly ScoringEngine _scoring = new();

    public HarvestEngine(IValidator<HarvestRequest>? validator = null)
    {
        _validator = validator ?? new HarvestRequestValidator();
    }

    public async Task<RunResult<HarvestResult>> ExecuteAsync(
        HarvestRequest request,
        IProgressReporter? progress = null,
        CancellationToken cancellationToken = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var validation = _validator.Validate(request);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .Select(e => new ErrorDetail(
                    $"harvest.{e.Field}",
                    e.Message))
                .ToList();
            return RunResult<HarvestResult>.Fail(errors);
        }

        // Aplica defaults se não informado
        if (request.IgnoredDirectories is null || request.IgnoredDirectories.Count == 0)
            request.IgnoredDirectories = HarvestDefaults.DefaultIgnoredDirectories;
        if (request.IgnoredExtensions is null || request.IgnoredExtensions.Count == 0)
            request.IgnoredExtensions = HarvestDefaults.DefaultIgnoredExtensions;
        if (request.IncludedExtensions is null || request.IncludedExtensions.Count == 0)
            request.IncludedExtensions = HarvestDefaults.DefaultIncludedExtensions;

        var issues = new List<ErrorDetail>();

        progress?.Report(new ProgressEvent("Enumerando arquivos", 5, "scan"));

        var filePaths = EnumerateFiles(request).ToList();
        var totalFiles = filePaths.Count;

        progress?.Report(new ProgressEvent("Lendo arquivos", 15, "scan"));

        var nodes = BuildFileNodes(request, filePaths, issues, progress, cancellationToken);

        progress?.Report(new ProgressEvent("Construindo grafo de dependências", 50, "graph"));

        // Extrai prefixes de using a ignorar — hardcoded pois não é campo do request (Sistema/Framework)
        var ignoreUsingPrefixes = new[] { "System", "Microsoft" };

        var graph = _graphBuilder.Build(
            nodes,
            ignoreUsingPrefixes,
            request.IncludedExtensions,
            request.RootPath,
            cancellationToken);

        progress?.Report(new ProgressEvent("Calculando scores", 70, "score"));

        var hits = new List<HarvestHit>();
        foreach (var file in nodes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var densities = KeywordAnalyzer.Analyze(
                file.Content,
                file.LineCount,
                request.Categories,
                request.DensityScale);

            var hit = _scoring.Score(file, graph, request, densities);
            if (hit.Score >= request.MinScore)
                hits.Add(hit);
        }

        var ordered = hits
            .OrderByDescending(h => h.Score)
            .ThenBy(h => h.File, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var initialIssueCount = issues.Count;
        if (request.CopyFiles && !string.IsNullOrWhiteSpace(request.OutputPath))
        {
            progress?.Report(new ProgressEvent("Copiando arquivos", 90, "copy"));
            CopyFiles(ordered, request.RootPath, request.OutputPath, issues);
        }

        var copyErrors = issues.Count - initialIssueCount;
        var changed = request.CopyFiles && !string.IsNullOrWhiteSpace(request.OutputPath)
            ? ordered.Count - copyErrors
            : 0;

        progress?.Report(new ProgressEvent("Concluído", 100, "done"));
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
            Ignored: totalFiles - hits.Count,
            Failed: issues.Count,
            Duration: sw.Elapsed);

        return RunResult<HarvestResult>.Success(new HarvestResult(report)).WithSummary(summary);
    }

    // ── Enumeração de arquivos ────────────────────────────────────────────────

    private static IEnumerable<string> EnumerateFiles(HarvestRequest request)
    {
        var allowedExt = new HashSet<string>(request.IncludedExtensions ?? [], StringComparer.OrdinalIgnoreCase);
        var excluded = new HashSet<string>(request.IgnoredDirectories ?? [], StringComparer.OrdinalIgnoreCase);
        var ignoredExt = new HashSet<string>(request.IgnoredExtensions ?? [], StringComparer.OrdinalIgnoreCase);

        var stack = new Stack<string>();
        stack.Push(request.RootPath);

        while (stack.Count > 0)
        {
            var dir = stack.Pop();

            IEnumerable<string> subDirs = [];
            IEnumerable<string> files = [];

            try
            {
                subDirs = Directory.EnumerateDirectories(dir, "*", SearchOption.TopDirectoryOnly);
                files = Directory.EnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly);
            }
            catch { continue; }

            foreach (var subDir in subDirs)
            {
                var name = Path.GetFileName(subDir);
                if (!excluded.Contains(name))
                    stack.Push(subDir);
            }

            foreach (var file in files)
            {
                var ext = Path.GetExtension(file);
                if (ignoredExt.Contains(ext)) continue;
                if (allowedExt.Count > 0 && !allowedExt.Contains(ext)) continue;
                yield return file;
            }
        }
    }

    // ── Construção de FileNodes ───────────────────────────────────────────────

    private List<FileNode> BuildFileNodes(
        HarvestRequest request,
        IReadOnlyList<string> filePaths,
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
                progress?.Report(new ProgressEvent(
                    $"Processando {idx}/{total}",
                    (int)(idx * 100.0 / Math.Max(1, total)),
                    "scan"));

            if (request.MaxFileSizeKb.HasValue)
            {
                try
                {
                    var info = new FileInfo(fullPath);
                    if (info.Length > request.MaxFileSizeKb.Value * 1024L)
                        continue;
                }
                catch (Exception ex)
                {
                    issues.Add(new ErrorDetail("harvest.file.size_error", "Falha ao ler tamanho do arquivo.", Cause: fullPath, Exception: ex));
                    continue;
                }
            }

            try
            {
                if (!TryReadText(fullPath, out var content))
                    continue;

                var extension = Path.GetExtension(fullPath);
                var relative = Path.GetRelativePath(request.RootPath, fullPath).Replace('\\', '/');
                var lineCount = CountEffectiveLines(content);

                string? ns = null;
                IReadOnlyList<string> types = [];
                var publicStaticCount = 0;

                if (extension.Equals(".cs", StringComparison.OrdinalIgnoreCase))
                {
                    ns = ExtractNamespace(content);
                    types = ExtractTypeNames(content);
                    publicStaticCount = CountPublicStatic(content);
                }

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
                issues.Add(new ErrorDetail("harvest.file.read_error", "Falha ao ler arquivo.", Cause: fullPath, Exception: ex));
            }
        }

        return nodes;
    }

    // ── Detecção de texto / encoding ─────────────────────────────────────────

    private static bool TryReadText(string path, out string content)
    {
        content = string.Empty;
        try
        {
            var bytes = File.ReadAllBytes(path);
            var detected = DetectText(bytes);
            if (detected.IsBinary) return false;
            content = detected.Content;
            return true;
        }
        catch { return false; }
    }

    private static DetectedText DetectText(byte[] bytes)
    {
        if (bytes.Length == 0)
            return new DetectedText(string.Empty, false);

        var bom = GetBomEncoding(bytes, out var bomLength);
        if (bom is not null)
            return new DetectedText(bom.GetString(bytes, bomLength, bytes.Length - bomLength), false);

        if (TryDecodeUtf8(bytes, out var utf8))
            return new DetectedText(utf8, false);

        if (LooksBinary(bytes))
            return new DetectedText(string.Empty, true);

        return new DetectedText(Encoding.GetEncoding(1252).GetString(bytes), false);
    }

    private static Encoding? GetBomEncoding(byte[] bytes, out int bomLength)
    {
        bomLength = 0;
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        { bomLength = 3; return new UTF8Encoding(true, true); }

        if (bytes.Length >= 4 && bytes[0] == 0xFF && bytes[1] == 0xFE && bytes[2] == 0x00 && bytes[3] == 0x00)
        { bomLength = 4; return new UTF32Encoding(false, true); }

        if (bytes.Length >= 4 && bytes[0] == 0x00 && bytes[1] == 0x00 && bytes[2] == 0xFE && bytes[3] == 0xFF)
        { bomLength = 4; return new UTF32Encoding(true, true); }

        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        { bomLength = 2; return new UnicodeEncoding(false, true); }

        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
        { bomLength = 2; return new UnicodeEncoding(true, true); }

        return null;
    }

    private static bool TryDecodeUtf8(byte[] bytes, out string content)
    {
        try
        {
            content = new UTF8Encoding(false, true).GetString(bytes);
            return true;
        }
        catch { content = string.Empty; return false; }
    }

    private static bool LooksBinary(byte[] bytes)
    {
        var sample = bytes.Length > 4096 ? bytes.AsSpan(0, 4096) : bytes.AsSpan();
        if (sample.Length == 0) return false;

        var controlCount = 0;
        foreach (var b in sample)
        {
            if (b == 0) return true;
            if (b < 9 || (b > 13 && b < 32)) controlCount++;
        }

        return (controlCount / (double)sample.Length) > 0.3;
    }

    private readonly record struct DetectedText(string Content, bool IsBinary);

    // ── Análise de C# ────────────────────────────────────────────────────────

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

    private static int CountPublicStatic(string content) =>
        PublicStaticRegex.Matches(content).Count;

    private static bool IsEntrypointCandidate(string fullPath, string content)
    {
        var fileName = Path.GetFileName(fullPath);
        if (fileName.Equals("Program.cs", StringComparison.OrdinalIgnoreCase)) return true;
        return MainMethodRegex.IsMatch(content);
    }

    private static int CountEffectiveLines(string content)
    {
        if (string.IsNullOrEmpty(content)) return 0;

        var count = 0;
        using var reader = new StringReader(content);
        string? line;
        var inBlockComment = false;

        while ((line = reader.ReadLine()) is not null)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) continue;

            if (inBlockComment)
            {
                var endIdx = trimmed.IndexOf("*/", StringComparison.Ordinal);
                if (endIdx >= 0)
                {
                    inBlockComment = false;
                    trimmed = trimmed[(endIdx + 2)..].Trim();
                    if (string.IsNullOrWhiteSpace(trimmed)) continue;
                }
                else continue;
            }

            if (trimmed.StartsWith("//", StringComparison.Ordinal)) continue;

            var startIdx = trimmed.IndexOf("/*", StringComparison.Ordinal);
            if (startIdx >= 0)
            {
                var endIdx = trimmed.IndexOf("*/", startIdx + 2, StringComparison.Ordinal);
                if (endIdx >= 0)
                {
                    trimmed = (trimmed[..startIdx] + trimmed[(endIdx + 2)..]).Trim();
                    if (string.IsNullOrWhiteSpace(trimmed)) continue;
                }
                else
                {
                    inBlockComment = true;
                    trimmed = trimmed[..startIdx].Trim();
                    if (string.IsNullOrWhiteSpace(trimmed)) continue;
                }
            }

            count++;
        }

        return count;
    }

    // ── Cópia de arquivos ────────────────────────────────────────────────────

    private static void CopyFiles(
        List<HarvestHit> hits,
        string rootPath,
        string outputPath,
        List<ErrorDetail> issues)
    {
        if (!Directory.Exists(outputPath))
        {
            try { Directory.CreateDirectory(outputPath); }
            catch (Exception ex)
            {
                issues.Add(new ErrorDetail(
                    "harvest.copy.create_dir_error",
                    "Falha ao criar pasta de destino.",
                    Cause: outputPath,
                    Action: "Verifique permissões ou validade do caminho.",
                    Exception: ex));
                return;
            }
        }

        foreach (var hit in hits)
        {
            try
            {
                var relativePath = Path.GetRelativePath(rootPath, hit.File);
                var destFile = Path.Combine(outputPath, relativePath);
                var destDir = Path.GetDirectoryName(destFile);

                if (destDir is not null && !Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir);

                File.Copy(hit.File, destFile, overwrite: true);
            }
            catch (Exception ex)
            {
                issues.Add(new ErrorDetail(
                    "harvest.copy.file_error",
                    "Falha ao copiar arquivo.",
                    Cause: hit.File,
                    Action: "Verifique acesso ao arquivo ou espaço em disco.",
                    Exception: ex));
            }
        }
    }
}
