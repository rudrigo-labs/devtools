using DevTools.Core.Abstractions;
using DevTools.Core.Models;
using DevTools.Core.Results;
using DevTools.Core.Providers;
using DevTools.Notes.Abstractions;
using DevTools.Notes.Models;
using DevTools.Notes.Providers;
using DevTools.Notes.Validation;

namespace DevTools.Notes.Engine;

public sealed class NotesEngine : IDevToolEngine<NotesRequest, NotesResponse>
{
    private readonly INotesStore? _store;
    private readonly NotesSimpleStore _simpleStore;
    private readonly NotesBackupStore _backupStore;

    public NotesEngine(INotesStore? store = null)
    {
        _store = store;
        _simpleStore = new NotesSimpleStore();
        _backupStore = new NotesBackupStore();
    }

    public async Task<RunResult<NotesResponse>> ExecuteAsync(
        NotesRequest request,
        IProgressReporter? progress = null,
        CancellationToken ct = default)
    {
        var errors = NotesRequestValidator.Validate(request);
        if (errors.Count > 0)
            return RunResult<NotesResponse>.Fail(errors);

        switch (request.Action)
        {
            case NotesAction.LoadNote:
                return await HandleLoadAsync(request, progress, ct).ConfigureAwait(false);

            case NotesAction.SaveNote:
                return await HandleSaveAsync(request, progress, ct).ConfigureAwait(false);

            case NotesAction.CreateItem:
                return await HandleCreateItemAsync(request, progress, ct).ConfigureAwait(false);

            case NotesAction.ListItems:
                return await HandleListItemsAsync(request, progress, ct).ConfigureAwait(false);

            case NotesAction.ExportZip:
                return HandleExportZip(request, progress);

            case NotesAction.ImportZip:
                return await HandleImportZipAsync(request, progress, ct).ConfigureAwait(false);

            default:
                return RunResult<NotesResponse>.Fail(new ErrorDetail("notes.action.invalid", "Action is invalid."));
        }
    }

    private async Task<RunResult<NotesResponse>> HandleCreateItemAsync(
        NotesRequest request,
        IProgressReporter? progress,
        CancellationToken ct)
    {
        progress?.Report(new ProgressEvent("Creating note", 20, "write"));

        var result = await _simpleStore.CreateAsync(
            request.NotesRootPath,
            request.Title!,
            request.Content!,
            request.LocalDate,
            request.UseMarkdown,
            request.CreateDateFolder,
            ct).ConfigureAwait(false);

        if (!result.IsSuccess || result.Value is null)
            return RunResult<NotesResponse>.Fail(result.Errors);

        progress?.Report(new ProgressEvent("Note created", 100, "done"));
        return RunResult<NotesResponse>.Success(new NotesResponse(
            request.Action,
            CreateResult: result.Value));
    }

    private async Task<RunResult<NotesResponse>> HandleListItemsAsync(
        NotesRequest request,
        IProgressReporter? progress,
        CancellationToken ct)
    {
        progress?.Report(new ProgressEvent("Loading index", 40, "read"));
        var result = await _simpleStore.ListAsync(request.NotesRootPath, ct).ConfigureAwait(false);
        if (!result.IsSuccess || result.Value is null)
            return RunResult<NotesResponse>.Fail(result.Errors);

        progress?.Report(new ProgressEvent("List ready", 100, "done"));
        return RunResult<NotesResponse>.Success(new NotesResponse(
            request.Action,
            ListResult: result.Value));
    }

    private RunResult<NotesResponse> HandleExportZip(NotesRequest request, IProgressReporter? progress)
    {
        progress?.Report(new ProgressEvent("Exporting zip", 30, "zip"));

        var root = NotesPaths.ResolveRoot(request.NotesRootPath);
        var result = _backupStore.ExportZip(root, request.OutputPath);
        if (!result.IsSuccess || result.Value is null)
            return RunResult<NotesResponse>.Fail(result.Errors);

        progress?.Report(new ProgressEvent("Exported", 100, "done"));
        return RunResult<NotesResponse>.Success(new NotesResponse(
            request.Action,
            ExportedZipPath: result.Value));
    }

    private async Task<RunResult<NotesResponse>> HandleImportZipAsync(
        NotesRequest request,
        IProgressReporter? progress,
        CancellationToken ct)
    {
        progress?.Report(new ProgressEvent("Extracting zip", 10, "zip"));

        var root = NotesPaths.ResolveRoot(request.NotesRootPath);
        var extract = _backupStore.ExtractZip(request.ZipPath!);
        if (!extract.IsSuccess)
            return RunResult<NotesResponse>.Fail(extract.Errors);

        var (tempDir, itemFiles, _) = extract.Value;
        try
        {
            var report = await MergeImportedAsync(root, tempDir, itemFiles, progress, ct).ConfigureAwait(false);
            if (!report.IsSuccess || report.Value is null)
                return RunResult<NotesResponse>.Fail(report.Errors);

            progress?.Report(new ProgressEvent("Import finished", 100, "done"));
            return RunResult<NotesResponse>.Success(new NotesResponse(
                request.Action,
                BackupReport: report.Value));
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { /* ignore */ }
        }
    }

    private async Task<RunResult<NotesBackupReport>> MergeImportedAsync(
        string root,
        string tempDir,
        List<string> itemFiles,
        IProgressReporter? progress,
        CancellationToken ct)
    {
        try
        {
            var fs = new SystemFileSystem();
            var indexStore = new NotesIndexStore(fs);
            var currentIndexResult = await indexStore.LoadAsync(root, ct).ConfigureAwait(false);
            if (!currentIndexResult.IsSuccess || currentIndexResult.Value is null)
                return RunResult<NotesBackupReport>.Fail(currentIndexResult.Errors);

            var index = currentIndexResult.Value;
            var rootItemsDir = NotesPaths.ItemsDir(root);
            fs.CreateDirectory(rootItemsDir);

            var imported = 0;
            var skipped = 0;
            var conflicts = new List<string>();

            var total = Math.Max(1, itemFiles.Count);
            for (var i = 0; i < itemFiles.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                var src = itemFiles[i];
                var rel = Path.GetRelativePath(Path.Combine(tempDir, "items"), src).Replace('\\', '/');
                var dst = Path.Combine(rootItemsDir, rel.Replace('/', Path.DirectorySeparatorChar));

                var dstDir = Path.GetDirectoryName(dst);
                if (!string.IsNullOrWhiteSpace(dstDir))
                    fs.CreateDirectory(dstDir);

                var srcText = await File.ReadAllTextAsync(src, ct).ConfigureAwait(false);
                var srcSha = NotesHash.Sha256Hex(srcText);

                if (!fs.FileExists(dst))
                {
                    fs.CopyFile(src, dst, overwrite: true);
                    imported++;
                }
                else
                {
                    var dstText = await fs.ReadAllTextAsync(dst, ct).ConfigureAwait(false);
                    var dstSha = NotesHash.Sha256Hex(dstText);

                    if (string.Equals(srcSha, dstSha, StringComparison.OrdinalIgnoreCase))
                    {
                        skipped++;
                    }
                    else
                    {
                        var conflictPath = BuildConflictPath(dst);
                        fs.CopyFile(src, conflictPath, overwrite: true);
                        conflicts.Add(Path.GetFileName(conflictPath));
                    }
                }

                var pct = (int)Math.Round(((i + 1) / (double)total) * 80) + 10;
                progress?.Report(new ProgressEvent("Importing notes", pct, "merge"));

                // Upsert index entry by FileName
                var existing = index.Items.FirstOrDefault(x => string.Equals(x.FileName, rel, StringComparison.OrdinalIgnoreCase));
                if (existing is null)
                {
                    index.Items.Add(new NotesIndexEntry
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        Title = InferTitleFromFileName(rel),
                        FileName = rel,
                        CreatedUtc = DateTime.UtcNow,
                        UpdatedUtc = DateTime.UtcNow,
                        Sha256 = srcSha
                    });
                }
                else
                {
                    existing.UpdatedUtc = DateTime.UtcNow;
                    existing.Sha256 = srcSha;
                }
            }

            var save = await indexStore.SaveAsync(root, index, ct).ConfigureAwait(false);
            if (!save.IsSuccess)
                return RunResult<NotesBackupReport>.Fail(save.Errors);

            return RunResult<NotesBackupReport>.Success(new NotesBackupReport(
                imported,
                skipped,
                conflicts.Count,
                conflicts));
        }
        catch (Exception ex)
        {
            return RunResult<NotesBackupReport>.Fail(new ErrorDetail(
                "notes.backup.import.merge.failed",
                "Failed to merge imported notes.",
                ex.Message,
                ex));
        }
    }

    private static string BuildConflictPath(string existingPath)
    {
        var dir = Path.GetDirectoryName(existingPath) ?? string.Empty;
        var name = Path.GetFileNameWithoutExtension(existingPath);
        var ext = Path.GetExtension(existingPath);
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        return Path.Combine(dir, $"{name} (conflict {stamp}){ext}");
    }

    private static string InferTitleFromFileName(string rel)
    {
        var file = Path.GetFileNameWithoutExtension(rel);
        // expected: YYYY-MM-DD - title
        var parts = file.Split(" - ", 2, StringSplitOptions.TrimEntries);
        return parts.Length == 2 ? parts[1] : file;
    }

    private async Task<RunResult<NotesResponse>> HandleLoadAsync(
        NotesRequest request,
        IProgressReporter? progress,
        CancellationToken ct)
    {
        var store = ResolveStore(request.NotesRootPath);
        progress?.Report(new ProgressEvent("Loading note", 30, "read"));

        var result = await store.ReadAsync(request.NoteKey!, ct).ConfigureAwait(false);
        if (!result.IsSuccess)
            return RunResult<NotesResponse>.Fail(result.Errors);

        progress?.Report(new ProgressEvent("Note loaded", 100, "done"));
        return RunResult<NotesResponse>.Success(new NotesResponse(
            request.Action,
            ReadResult: result.Value));
    }

    private async Task<RunResult<NotesResponse>> HandleSaveAsync(
        NotesRequest request,
        IProgressReporter? progress,
        CancellationToken ct)
    {
        var store = ResolveStore(request.NotesRootPath);
        progress?.Report(new ProgressEvent("Saving note", 30, "write"));

        var result = await store.WriteAsync(request.NoteKey!, request.Content!, request.Overwrite, ct)
            .ConfigureAwait(false);
        if (!result.IsSuccess)
            return RunResult<NotesResponse>.Fail(result.Errors);

        progress?.Report(new ProgressEvent("Note saved", 100, "done"));
        return RunResult<NotesResponse>.Success(new NotesResponse(
            request.Action,
            WriteResult: result.Value));
    }

    private INotesStore ResolveStore(string? rootPath)
        => _store ?? new SystemNotesStore(rootPath ?? string.Empty);
}
