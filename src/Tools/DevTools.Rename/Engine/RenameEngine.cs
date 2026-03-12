using DevTools.Core.Abstractions;
using DevTools.Core.Models;
using DevTools.Core.Results;
using DevTools.Core.Validation;
using DevTools.Rename.Abstractions;
using DevTools.Rename.Models;
using DevTools.Rename.Providers;
using DevTools.Rename.Validators;

namespace DevTools.Rename.Engine;

public sealed class RenameEngine : IDevToolEngine<RenameRequest, RenameResult>
{
    private readonly IRenameFileSystem _fs;
    private readonly DirectoryScanner _scanner;
    private readonly IValidator<RenameRequest> _validator;
    private readonly AppSettings _settings;

    public RenameEngine(
        AppSettings? settings = null,
        IRenameFileSystem? fileSystem = null,
        IValidator<RenameRequest>? validator = null)
    {
        _settings = settings ?? new AppSettings();
        _fs = fileSystem ?? new SystemRenameFileSystem();
        _scanner = new DirectoryScanner(_fs);
        _validator = validator ?? new RenameRequestValidator(_settings);
    }

    public async Task<RunResult<RenameResult>> ExecuteAsync(
        RenameRequest request,
        IProgressReporter? progress = null,
        CancellationToken ct = default)
    {
        // Aplica defaults do AppSettings se não informado
        request.MaxFileSizeKb ??= _settings.FileTools.MaxFileSizeKb;

        if (request.IgnoredDirectories is null || request.IgnoredDirectories.Count == 0)
            request.IgnoredDirectories = RenameDefaults.DefaultIgnoredDirectories;
        if (request.IgnoredExtensions is null || request.IgnoredExtensions.Count == 0)
            request.IgnoredExtensions = RenameDefaults.DefaultIgnoredExtensions;
        if (request.IncludedExtensions is null || request.IncludedExtensions.Count == 0)
            request.IncludedExtensions = RenameDefaults.DefaultIncludedExtensions;

        var validation = _validator.Validate(request);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .Select(x => new ErrorDetail(x.Code ?? "VALIDATION_ERROR", x.Message, x.Field))
                .ToArray();
            return RunResult<RenameResult>.Fail(errors);
        }

        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var rootPath = Path.GetFullPath(request.RootPath);
            var scan = _scanner.Scan(rootPath, request);

            var changes = new List<RenameChange>();
            var diffs = new List<FileDiffSummary>();
            var errors = new List<ErrorDetail>();

            var filesScanned = 0;
            var directoriesScanned = 0;
            var filesUpdated = 0;
            var filesRenamed = 0;
            var directoriesRenamed = 0;
            var skippedBinary = 0;
            var skippedExists = 0;

            var total = scan.Files.Count + scan.Directories.Count;
            var step = 0;

            foreach (var file in scan.Files)
            {
                ct.ThrowIfCancellationRequested();
                filesScanned++;
                step++;
                progress?.Report(new Core.Models.ProgressEvent("Processando arquivo", Percent(step, total), "file"));

                try
                {
                    var stats = await ProcessFileAsync(request, rootPath, file, changes, diffs, ct).ConfigureAwait(false);
                    filesUpdated += stats.FilesUpdated;
                    filesRenamed += stats.FilesRenamed;
                    skippedBinary += stats.SkippedBinary;
                    skippedExists += stats.SkippedExists;
                }
                catch (Exception ex)
                {
                    var relative = Normalize(Path.GetRelativePath(rootPath, file));
                    errors.Add(new ErrorDetail("rename.file.failed", "Falha ao processar arquivo.", relative, Exception: ex));
                }
            }

            var dirList = scan.Directories.OrderByDescending(d => d.Length).ToList();
            var segmentMap = BuildNamespaceSegmentMap(request);

            foreach (var dir in dirList)
            {
                ct.ThrowIfCancellationRequested();
                directoriesScanned++;
                step++;
                progress?.Report(new Core.Models.ProgressEvent("Processando diretorio", Percent(step, total), "dir"));

                try
                {
                    if (TryRenameDirectory(request, rootPath, dir, segmentMap, out var newPath))
                    {
                        if (request.DryRun)
                        {
                            changes.Add(new RenameChange(RenameChangeType.DirectoryRenamed, dir, newPath));
                            directoriesRenamed++;
                        }
                        else if (_fs.DirectoryExists(newPath))
                        {
                            skippedExists++;
                            errors.Add(new ErrorDetail("rename.dir.exists", "Diretorio destino ja existe.", newPath));
                        }
                        else
                        {
                            _fs.MoveDirectory(dir, newPath);
                            changes.Add(new RenameChange(RenameChangeType.DirectoryRenamed, dir, newPath));
                            directoriesRenamed++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    var relative = Normalize(Path.GetRelativePath(rootPath, dir));
                    errors.Add(new ErrorDetail("rename.dir.failed", "Falha ao renomear diretorio.", relative, Exception: ex));
                }
            }

            progress?.Report(new Core.Models.ProgressEvent("Concluido", 100, "done"));
            sw.Stop();

            var summary = new RenameSummary(
                filesScanned, directoriesScanned,
                filesUpdated, filesRenamed, directoriesRenamed,
                skippedBinary, skippedExists, errors.Count);

            string? undoLogPath = null;
            if (request.WriteUndoLog)
            {
                undoLogPath = ResolveUndoLogPath(request.UndoLogPath, rootPath);
                try
                {
                    var undoLog = new RenameUndoLog(DateTimeOffset.UtcNow, changes
                        .Where(c => c.Type is RenameChangeType.ContentUpdated or RenameChangeType.FileRenamed or RenameChangeType.DirectoryRenamed)
                        .Select(c => new RenameUndoOperation(c.Type, c.Path, c.NewPath, c.BackupPath))
                        .ToList());
                    RenameReportWriter.WriteUndoLog(undoLogPath, undoLog);
                }
                catch (Exception ex)
                {
                    errors.Add(new ErrorDetail("rename.undo.failed", "Falha ao gravar undo log.", undoLogPath, Exception: ex));
                }
            }

            string? reportPath = null;
            if (!string.IsNullOrWhiteSpace(request.ReportPath))
            {
                reportPath = Path.GetFullPath(request.ReportPath);
                try
                {
                    var report = new RenameReport(
                        DateTimeOffset.UtcNow, request, summary, changes, diffs,
                        errors.Select(e => new RenameReportError(e.Code, e.Message, e.Details)).ToList(),
                        undoLogPath);
                    RenameReportWriter.Write(reportPath, report);
                }
                catch (Exception ex)
                {
                    errors.Add(new ErrorDetail("rename.report.failed", "Falha ao gravar report.", reportPath, Exception: ex));
                }
            }

            var result = new RenameResult(summary, changes, diffs, reportPath, undoLogPath);

            var runSummary = new RunSummary(
                ToolName: "Rename",
                Mode: request.DryRun ? "DryRun" : "Real",
                MainInput: request.RootPath,
                OutputLocation: null,
                Processed: filesScanned + directoriesScanned,
                Changed: filesUpdated + filesRenamed + directoriesRenamed,
                Ignored: skippedBinary + skippedExists,
                Failed: errors.Count,
                Duration: sw.Elapsed);

            return RunResult<RenameResult>.Success(result).WithSummary(runSummary);
        }
        catch (OperationCanceledException)
        {
            return RunResult<RenameResult>.Fail(new ErrorDetail("CANCELLED", "Execucao cancelada pelo usuario."));
        }
        catch (Exception ex)
        {
            return RunResult<RenameResult>.FromException("RENAME_ERROR", $"Erro durante execucao: {ex.Message}", ex);
        }
    }

    private async Task<FileProcessStats> ProcessFileAsync(
        RenameRequest request, string rootPath, string file,
        List<RenameChange> changes, List<FileDiffSummary> diffs, CancellationToken ct)
    {
        var stats = new FileProcessStats();
        var detected = await TextFileHelper.ReadAsync(_fs, file, ct).ConfigureAwait(false);
        if (detected.IsBinary)
        {
            changes.Add(new RenameChange(RenameChangeType.SkippedBinary, file));
            stats.SkippedBinary++;
            return stats;
        }

        var content = detected.Content;
        var newContent = ApplyRenameToContent(request, file, content);

        if (!string.Equals(content, newContent, StringComparison.Ordinal))
        {
            var relative = Path.GetRelativePath(rootPath, file);
            diffs.Add(DiffGenerator.Generate(relative, content, newContent, request.MaxDiffLinesPerFile));

            if (!request.DryRun)
            {
                string? backupPath = null;
                if (request.BackupEnabled)
                {
                    backupPath = CreateBackupPath(file);
                    _fs.CopyFile(file, backupPath, overwrite: false);
                }
                await TextFileHelper.WriteAsync(_fs, file, newContent, detected, ct).ConfigureAwait(false);
                changes.Add(new RenameChange(RenameChangeType.ContentUpdated, file, BackupPath: backupPath));
            }
            else
            {
                changes.Add(new RenameChange(RenameChangeType.ContentUpdated, file));
            }

            stats.FilesUpdated++;
        }

        if (TryRenameFile(request, file, out var newPath))
        {
            if (request.DryRun)
            {
                changes.Add(new RenameChange(RenameChangeType.FileRenamed, file, newPath));
                stats.FilesRenamed++;
            }
            else if (_fs.FileExists(newPath))
            {
                changes.Add(new RenameChange(RenameChangeType.SkippedExists, file, newPath));
                stats.SkippedExists++;
            }
            else
            {
                _fs.MoveFile(file, newPath);
                changes.Add(new RenameChange(RenameChangeType.FileRenamed, file, newPath));
                stats.FilesRenamed++;
            }
        }

        return stats;
    }

    private static string ApplyRenameToContent(RenameRequest request, string file, string content)
    {
        var extension = Path.GetExtension(file);
        var isCSharp = extension.Equals(".cs", StringComparison.OrdinalIgnoreCase);

        if (isCSharp)
            return RoslynRenamer.Rename(content, request.OldText, request.NewText, request.Mode);

        if (request.Mode == RenameMode.NamespaceOnly)
        {
            if (IsNamespaceFile(extension))
                return content.Replace(request.OldText, request.NewText, StringComparison.Ordinal);
            return content;
        }

        return content.Replace(request.OldText, request.NewText, StringComparison.Ordinal);
    }

    private static bool IsNamespaceFile(string extension) =>
        extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase) ||
        extension.Equals(".sln", StringComparison.OrdinalIgnoreCase) ||
        extension.Equals(".props", StringComparison.OrdinalIgnoreCase) ||
        extension.Equals(".targets", StringComparison.OrdinalIgnoreCase);

    private static bool TryRenameFile(RenameRequest request, string file, out string newPath)
    {
        newPath = file;
        var fileName = Path.GetFileName(file);
        var extension = Path.GetExtension(file);

        if (request.Mode == RenameMode.NamespaceOnly)
        {
            if (!extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase)) return false;
            if (!fileName.Contains(request.OldText, StringComparison.Ordinal)) return false;
            var newFileName = fileName.Replace(request.OldText, request.NewText, StringComparison.Ordinal);
            newPath = Path.Combine(Path.GetDirectoryName(file)!, newFileName);
            return !string.Equals(newPath, file, StringComparison.Ordinal);
        }

        if (!fileName.Contains(request.OldText, StringComparison.Ordinal)) return false;
        var replaced = fileName.Replace(request.OldText, request.NewText, StringComparison.Ordinal);
        newPath = Path.Combine(Path.GetDirectoryName(file)!, replaced);
        return !string.Equals(newPath, file, StringComparison.Ordinal);
    }

    private static bool TryRenameDirectory(
        RenameRequest request, string rootPath, string dir,
        IReadOnlyDictionary<string, string> segmentMap, out string newPath)
    {
        newPath = dir;
        var dirName = Path.GetFileName(dir);

        if (request.Mode == RenameMode.NamespaceOnly)
        {
            if (!segmentMap.TryGetValue(dirName, out var mapped)) return false;
            if (string.Equals(dirName, mapped, StringComparison.Ordinal)) return false;
            var parent = Path.GetDirectoryName(dir) ?? rootPath;
            newPath = Path.Combine(parent, mapped);
            return !string.Equals(newPath, dir, StringComparison.Ordinal);
        }

        if (!dirName.Contains(request.OldText, StringComparison.Ordinal)) return false;
        var newName = dirName.Replace(request.OldText, request.NewText, StringComparison.Ordinal);
        var parentDir = Path.GetDirectoryName(dir) ?? rootPath;
        newPath = Path.Combine(parentDir, newName);
        return !string.Equals(newPath, dir, StringComparison.Ordinal);
    }

    private static IReadOnlyDictionary<string, string> BuildNamespaceSegmentMap(RenameRequest request)
    {
        if (request.Mode != RenameMode.NamespaceOnly && !request.OldText.Contains('.'))
            return new Dictionary<string, string>();

        var oldParts = request.OldText.Split('.', StringSplitOptions.RemoveEmptyEntries);
        var newParts = request.NewText.Split('.', StringSplitOptions.RemoveEmptyEntries);
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        var count = Math.Min(oldParts.Length, newParts.Length);
        for (int i = 0; i < count; i++)
            if (!map.ContainsKey(oldParts[i])) map[oldParts[i]] = newParts[i];

        return map;
    }

    private static string ResolveUndoLogPath(string? path, string rootPath) =>
        !string.IsNullOrWhiteSpace(path) ? Path.GetFullPath(path) : Path.Combine(rootPath, "rename-undo.json");

    private static string CreateBackupPath(string path)
    {
        var basePath = path + ".bak";
        if (!File.Exists(basePath)) return basePath;
        var index = 1;
        while (true)
        {
            var candidate = basePath + index;
            if (!File.Exists(candidate)) return candidate;
            index++;
        }
    }

    private static int? Percent(int step, int total) =>
        total <= 0 ? null : (int)Math.Round(step * 100d / total);

    private static string Normalize(string path) => path.Replace('\\', '/');

    private sealed class FileProcessStats
    {
        public int FilesUpdated { get; set; }
        public int FilesRenamed { get; set; }
        public int SkippedBinary { get; set; }
        public int SkippedExists { get; set; }
    }
}
