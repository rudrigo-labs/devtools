using System.Text;
using System.Text.Json;
using DevTools.Core.Abstractions;
using DevTools.Core.Models;
using DevTools.Core.Results;
using DevTools.Core.Validation;
using DevTools.Snapshot.Models;
using DevTools.Snapshot.Validators;

namespace DevTools.Snapshot.Engine;

public sealed class SnapshotEngine : IDevToolEngine<SnapshotRequest, SnapshotResult>
{
    private readonly IValidator<SnapshotRequest> _validator;
    private readonly AppSettings _settings;

    public SnapshotEngine(AppSettings? settings = null, IValidator<SnapshotRequest>? validator = null)
    {
        _settings = settings ?? new AppSettings();
        _validator = validator ?? new SnapshotRequestValidator(_settings);
    }

    public async Task<RunResult<SnapshotResult>> ExecuteAsync(
        SnapshotRequest request,
        IProgressReporter? progress = null,
        CancellationToken cancellationToken = default)
    {
        // Aplica defaults do AppSettings se não informado
        request.MaxFileSizeKb ??= _settings.FileTools.MaxFileSizeKb;

        if (request.IgnoredDirectories is null || request.IgnoredDirectories.Count == 0)
            request.IgnoredDirectories = SnapshotDefaults.DefaultIgnoredDirectories;
        if (request.IgnoredExtensions is null || request.IgnoredExtensions.Count == 0)
            request.IgnoredExtensions = SnapshotDefaults.DefaultIgnoredExtensions;

        var validation = _validator.Validate(request);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .Select(x => new ErrorDetail(
                    Code: x.Code ?? "VALIDATION_ERROR",
                    Message: x.Message,
                    Cause: x.Field))
                .ToArray();
            return RunResult<SnapshotResult>.Fail(errors);
        }

        try
        {
            progress?.Report(new ProgressEvent("Escaneando arquivos...", 5, "snapshot"));
            cancellationToken.ThrowIfCancellationRequested();

            var files = CollectFiles(request, cancellationToken);
            var totalScanned = files.Count;

            progress?.Report(new ProgressEvent($"{totalScanned} arquivos encontrados. Gerando artefatos...", 30, "snapshot"));

            var outputDir = Path.Combine(request.OutputBasePath, $"snapshot_{DateTime.Now:yyyyMMdd_HHmmss}");
            Directory.CreateDirectory(outputDir);

            var artifacts = new List<string>();
            int step = 30;
            int activeFormats = (request.GenerateText ? 1 : 0)
                + (request.GenerateHtmlPreview ? 1 : 0)
                + (request.GenerateJsonNested ? 1 : 0)
                + (request.GenerateJsonRecursive ? 1 : 0);
            int stepSize = activeFormats > 0 ? 60 / activeFormats : 0;

            if (request.GenerateText)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var path = await GenerateTextAsync(files, request.RootPath, outputDir, cancellationToken);
                artifacts.Add(path);
                step += stepSize;
                progress?.Report(new ProgressEvent("Artefato .txt gerado.", step, "snapshot"));
            }

            if (request.GenerateHtmlPreview)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var path = await GenerateHtmlAsync(files, request.RootPath, outputDir, cancellationToken);
                artifacts.Add(path);
                step += stepSize;
                progress?.Report(new ProgressEvent("Artefato .html gerado.", step, "snapshot"));
            }

            if (request.GenerateJsonNested)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var path = await GenerateJsonNestedAsync(files, request.RootPath, outputDir, cancellationToken);
                artifacts.Add(path);
                step += stepSize;
                progress?.Report(new ProgressEvent("Artefato JSON aninhado gerado.", step, "snapshot"));
            }

            if (request.GenerateJsonRecursive)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var path = await GenerateJsonRecursiveAsync(files, request.RootPath, outputDir, cancellationToken);
                artifacts.Add(path);
                step += stepSize;
                progress?.Report(new ProgressEvent("Artefato JSON recursivo gerado.", step, "snapshot"));
            }

            progress?.Report(new ProgressEvent("Snapshot concluido.", 100, "snapshot"));

            return RunResult<SnapshotResult>.Success(new SnapshotResult
            {
                RootPath = request.RootPath,
                GeneratedArtifacts = artifacts,
                TotalFilesScanned = totalScanned,
                TotalFilesIncluded = files.Count
            });
        }
        catch (OperationCanceledException)
        {
            return RunResult<SnapshotResult>.Fail(
                new ErrorDetail("CANCELLED", "Execucao cancelada pelo usuario."));
        }
        catch (Exception ex)
        {
            return RunResult<SnapshotResult>.FromException(
                "SNAPSHOT_ERROR", $"Erro durante execucao: {ex.Message}", ex);
        }
    }

    // -------------------------------------------------------------------------
    // Coleta de arquivos
    // -------------------------------------------------------------------------

    private List<string> CollectFiles(SnapshotRequest request, CancellationToken ct)
    {
        var ignoredDirs = new HashSet<string>(
            request.IgnoredDirectories.Select(d => d.Trim()),
            StringComparer.OrdinalIgnoreCase);

        var ignoredExts = new HashSet<string>(
            request.IgnoredExtensions.Select(e => e.Trim()),
            StringComparer.OrdinalIgnoreCase);

        long maxBytes = (request.MaxFileSizeKb ?? _settings.FileTools.MaxFileSizeKb) * 1024L;

        var result = new List<string>();
        CollectRecursive(request.RootPath, ignoredDirs, ignoredExts, maxBytes, result, ct);
        return result;
    }

    private static void CollectRecursive(
        string dir,
        HashSet<string> ignoredDirs,
        HashSet<string> ignoredExts,
        long maxBytes,
        List<string> result,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            foreach (var subDir in Directory.EnumerateDirectories(dir))
            {
                var name = Path.GetFileName(subDir);
                if (ignoredDirs.Contains(name))
                    continue;
                CollectRecursive(subDir, ignoredDirs, ignoredExts, maxBytes, result, ct);
            }

            foreach (var file in Directory.EnumerateFiles(dir))
            {
                ct.ThrowIfCancellationRequested();
                var ext = Path.GetExtension(file);
                if (ignoredExts.Contains(ext))
                    continue;
                var info = new FileInfo(file);
                if (info.Length > maxBytes)
                    continue;
                result.Add(file);
            }
        }
        catch (UnauthorizedAccessException) { }
        catch (PathTooLongException) { }
    }

    // -------------------------------------------------------------------------
    // Geradores de artefatos
    // -------------------------------------------------------------------------

    private static async Task<string> GenerateTextAsync(
        List<string> files, string rootPath, string outputDir, CancellationToken ct)
    {
        var outputPath = Path.Combine(outputDir, "snapshot.txt");
        await using var writer = new StreamWriter(outputPath, false, Encoding.UTF8);

        await writer.WriteLineAsync($"# Snapshot gerado em {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        await writer.WriteLineAsync($"# Raiz: {rootPath}");
        await writer.WriteLineAsync($"# Total de arquivos: {files.Count}");
        await writer.WriteLineAsync();

        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();
            var relative = Path.GetRelativePath(rootPath, file);
            await writer.WriteLineAsync($"=== {relative} ===");
            try
            {
                var content = await File.ReadAllTextAsync(file, ct);
                await writer.WriteLineAsync(content);
            }
            catch
            {
                await writer.WriteLineAsync("[Nao foi possivel ler o arquivo]");
            }
            await writer.WriteLineAsync();
        }

        return outputPath;
    }

    private static async Task<string> GenerateHtmlAsync(
        List<string> files, string rootPath, string outputDir, CancellationToken ct)
    {
        var outputPath = Path.Combine(outputDir, "snapshot.html");
        await using var writer = new StreamWriter(outputPath, false, Encoding.UTF8);

        await writer.WriteLineAsync("<!DOCTYPE html>");
        await writer.WriteLineAsync("<html lang=\"pt-BR\"><head><meta charset=\"UTF-8\">");
        await writer.WriteLineAsync("<title>Snapshot do Projeto</title><style>");
        await writer.WriteLineAsync("body{font-family:Consolas,monospace;background:#1e1e1e;color:#d4d4d4;margin:0;padding:20px}");
        await writer.WriteLineAsync("h1{color:#569cd6;border-bottom:1px solid #333;padding-bottom:8px}");
        await writer.WriteLineAsync(".file{margin-bottom:24px}");
        await writer.WriteLineAsync(".file-header{background:#252526;color:#9cdcfe;padding:8px 12px;border-radius:4px 4px 0 0;font-weight:bold}");
        await writer.WriteLineAsync("pre{margin:0;padding:12px;background:#1e1e1e;border:1px solid #333;border-top:none;border-radius:0 0 4px 4px;overflow-x:auto;white-space:pre-wrap;word-break:break-word}");
        await writer.WriteLineAsync("</style></head><body>");
        await writer.WriteLineAsync($"<h1>Snapshot &mdash; {HtmlEncode(rootPath)}</h1>");
        await writer.WriteLineAsync($"<p style='color:#858585'>Gerado em {DateTime.Now:yyyy-MM-dd HH:mm:ss} &bull; {files.Count} arquivos</p>");

        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();
            var relative = Path.GetRelativePath(rootPath, file);
            await writer.WriteLineAsync($"<div class='file'><div class='file-header'>{HtmlEncode(relative)}</div><pre>");
            try
            {
                var content = await File.ReadAllTextAsync(file, ct);
                await writer.WriteAsync(HtmlEncode(content));
            }
            catch
            {
                await writer.WriteAsync("[Nao foi possivel ler o arquivo]");
            }
            await writer.WriteLineAsync("</pre></div>");
        }

        await writer.WriteLineAsync("</body></html>");
        return outputPath;
    }

    private static async Task<string> GenerateJsonNestedAsync(
        List<string> files, string rootPath, string outputDir, CancellationToken ct)
    {
        var root = BuildNestedTree(files, rootPath, ct);
        var outputPath = Path.Combine(outputDir, "snapshot-nested.json");
        var json = JsonSerializer.Serialize(root, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(outputPath, json, Encoding.UTF8, ct);
        return outputPath;
    }

    private static async Task<string> GenerateJsonRecursiveAsync(
        List<string> files, string rootPath, string outputDir, CancellationToken ct)
    {
        var entries = new List<object>(files.Count);
        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();
            var relative = Path.GetRelativePath(rootPath, file);
            string content;
            try { content = await File.ReadAllTextAsync(file, ct); }
            catch { content = "[Nao foi possivel ler o arquivo]"; }
            entries.Add(new { path = relative, content });
        }

        var outputPath = Path.Combine(outputDir, "snapshot-recursive.json");
        var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(outputPath, json, Encoding.UTF8, ct);
        return outputPath;
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static Dictionary<string, object> BuildNestedTree(
        List<string> files, string rootPath, CancellationToken ct)
    {
        var root = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();
            var relative = Path.GetRelativePath(rootPath, file);
            var parts = relative.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });
            var current = root;

            for (int i = 0; i < parts.Length - 1; i++)
            {
                if (!current.TryGetValue(parts[i], out var child) ||
                    child is not Dictionary<string, object> childDict)
                {
                    childDict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    current[parts[i]] = childDict;
                }
                current = (Dictionary<string, object>)current[parts[i]];
            }

            string fileContent;
            try { fileContent = File.ReadAllText(file); }
            catch { fileContent = "[Nao foi possivel ler o arquivo]"; }
            current[parts[^1]] = fileContent;
        }

        return root;
    }

    private static string HtmlEncode(string text) =>
        text.Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");
}
