using DevTools.Core.Abstractions;
using DevTools.Core.Models;
using DevTools.Core.Results;
using DevTools.Core.Utilities;
using DevTools.Notes.Abstractions;
using DevTools.Notes.Models;
using DevTools.Notes.Providers;
using DevTools.Notes.Repositories;
using DevTools.Notes.Validators;

namespace DevTools.Notes.Engine;

public sealed class NotesEngine : IDevToolEngine<NotesRequest, NotesResponse>
{
    private readonly INotesItemsRepository _itemsRepository;
    private readonly NotesBackupStore _backupStore;
    private readonly IGoogleDriveNotesStore? _driveStore;

    public NotesEngine(
        INotesItemsRepository itemsRepository,
        NotesBackupStore backupStore,
        IGoogleDriveNotesStore? driveStore = null)
    {
        _itemsRepository = itemsRepository;
        _backupStore = backupStore;
        _driveStore = driveStore;
    }

    public async Task<RunResult<NotesResponse>> ExecuteAsync(
        NotesRequest request,
        IProgressReporter? progress = null,
        CancellationToken ct = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var validation = new NotesRequestValidator().Validate(request);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .Select(e => new ErrorDetail($"notes.{e.Field}", e.Message))
                .ToList();
            return RunResult<NotesResponse>.Fail(errors);
        }

        RunResult<NotesResponse> result;

        try
        {
            result = request.Action switch
            {
                NotesAction.LoadNote   => await HandleLoadAsync(request, progress, ct),
                NotesAction.SaveNote   => await HandleSaveAsync(request, progress, ct),
                NotesAction.CreateItem => await HandleCreateItemAsync(request, progress, ct),
                NotesAction.ListItems  => await HandleListItemsAsync(request, progress, ct),
                NotesAction.ExportZip  => HandleExportZip(request, progress),
                NotesAction.ImportZip  => await HandleImportZipAsync(request, progress, ct),
                NotesAction.DeleteItem => await HandleDeleteItemAsync(request, progress, ct),
                _ => RunResult<NotesResponse>.Fail(new ErrorDetail("notes.action.invalid", "Action inválida."))
            };
        }
        catch (Exception ex)
        {
            result = RunResult<NotesResponse>.Fail(new ErrorDetail("notes.engine.crash", "Erro inesperado.", Exception: ex));
        }

        sw.Stop();
        return result.WithSummary(BuildSummary(request, result, sw.Elapsed));
    }

    private async Task<RunResult<NotesResponse>> HandleCreateItemAsync(
        NotesRequest request, IProgressReporter? progress, CancellationToken ct)
    {
        progress?.Report(new ProgressEvent("Criando nota", 20, "write"));

        var ext = ResolveExtension(request.Extension);
        var useMarkdown = ext == ".md";

        var result = await _itemsRepository.CreateAsync(
            request.NotesRootPath, request.Title!, request.Content!,
            request.LocalDate, useMarkdown, request.CreateDateFolder, ct);

        if (!result.IsSuccess || result.Value is null)
            return RunResult<NotesResponse>.Fail(result.Errors);

        progress?.Report(new ProgressEvent("Sincronizando Drive", 70, "sync"));

        var (driveSkipped, skipReason) = await TrySyncToDriveAsync(
            result.Value.FileName, result.Value.Path, ext, ct);

        progress?.Report(new ProgressEvent("Concluído", 100, "done"));

        return RunResult<NotesResponse>.Success(new NotesResponse(request.Action)
        {
            CreateResult   = result.Value,
            DriveSkipped   = driveSkipped,
            DriveSkipReason = skipReason
        });
    }

    private async Task<RunResult<NotesResponse>> HandleListItemsAsync(
        NotesRequest request, IProgressReporter? progress, CancellationToken ct)
    {
        progress?.Report(new ProgressEvent("Carregando índice", 40, "read"));
        var result = await _itemsRepository.ListAsync(request.NotesRootPath, ct);
        if (!result.IsSuccess || result.Value is null)
            return RunResult<NotesResponse>.Fail(result.Errors);

        progress?.Report(new ProgressEvent("Lista pronta", 100, "done"));
        return RunResult<NotesResponse>.Success(new NotesResponse(request.Action) { ListResult = result.Value });
    }

    private RunResult<NotesResponse> HandleExportZip(NotesRequest request, IProgressReporter? progress)
    {
        progress?.Report(new ProgressEvent("Exportando zip", 30, "zip"));
        var root = NotesPaths.ResolveRoot(request.NotesRootPath);
        var result = _backupStore.ExportZip(root, request.OutputPath);
        if (!result.IsSuccess || result.Value is null)
            return RunResult<NotesResponse>.Fail(result.Errors);

        progress?.Report(new ProgressEvent("Exportado", 100, "done"));
        return RunResult<NotesResponse>.Success(new NotesResponse(request.Action) { ExportedZipPath = result.Value });
    }

    private async Task<RunResult<NotesResponse>> HandleImportZipAsync(
        NotesRequest request, IProgressReporter? progress, CancellationToken ct)
    {
        progress?.Report(new ProgressEvent("Extraindo zip", 10, "zip"));
        var root = NotesPaths.ResolveRoot(request.NotesRootPath);
        var extract = _backupStore.ExtractZip(request.ZipPath!);
        if (!extract.IsSuccess) return RunResult<NotesResponse>.Fail(extract.Errors);

        var (tempDir, itemFiles, _) = extract.Value;
        try
        {
            var report = await MergeImportedAsync(root, tempDir, itemFiles, progress, ct);
            if (!report.IsSuccess || report.Value is null)
                return RunResult<NotesResponse>.Fail(report.Errors);

            progress?.Report(new ProgressEvent("Importação concluída", 100, "done"));
            return RunResult<NotesResponse>.Success(new NotesResponse(request.Action) { BackupReport = report.Value });
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { /* ignore */ }
        }
    }

    private async Task<RunResult<NotesResponse>> HandleLoadAsync(
        NotesRequest request, IProgressReporter? progress, CancellationToken ct)
    {
        progress?.Report(new ProgressEvent("Carregando nota", 30, "read"));
        var result = await _itemsRepository.ReadAsync(request.NotesRootPath, request.NoteKey!, ct);
        if (!result.IsSuccess) return RunResult<NotesResponse>.Fail(result.Errors);

        progress?.Report(new ProgressEvent("Nota carregada", 100, "done"));
        return RunResult<NotesResponse>.Success(new NotesResponse(request.Action) { ReadResult = result.Value });
    }

    private async Task<RunResult<NotesResponse>> HandleSaveAsync(
        NotesRequest request, IProgressReporter? progress, CancellationToken ct)
    {
        progress?.Report(new ProgressEvent("Salvando nota", 30, "write"));

        var write = await _itemsRepository.WriteAsync(
            request.NotesRootPath, request.NoteKey!, request.Content!, request.Overwrite, ct);

        if (!write.IsSuccess || write.Value is null)
            return RunResult<NotesResponse>.Fail(write.Errors);

        var sync = await SyncIndexAfterSimpleWriteAsync(
            request.NotesRootPath, request.NoteKey!, request.Content!, ct);
        if (!sync.IsSuccess) return RunResult<NotesResponse>.Fail(sync.Errors);

        progress?.Report(new ProgressEvent("Sincronizando Drive", 70, "sync"));

        var ext = Path.GetExtension(request.NoteKey) is ".md" or ".txt"
            ? Path.GetExtension(request.NoteKey)! : ".txt";

        var (driveSkipped, skipReason) = await TrySyncToDriveAsync(
            request.NoteKey!, write.Value.Path, ext, ct);

        progress?.Report(new ProgressEvent("Concluído", 100, "done"));
        return RunResult<NotesResponse>.Success(new NotesResponse(request.Action)
        {
            WriteResult    = write.Value,
            DriveSkipped   = driveSkipped,
            DriveSkipReason = skipReason
        });
    }

    private async Task<RunResult<NotesResponse>> HandleDeleteItemAsync(
        NotesRequest request, IProgressReporter? progress, CancellationToken ct)
    {
        progress?.Report(new ProgressEvent("Deletando nota", 40, "delete"));

        var result = await _itemsRepository.DeleteAsync(request.NotesRootPath, request.NoteKey!, ct);
        if (!result.IsSuccess || result.Value is null)
            return RunResult<NotesResponse>.Fail(result.Errors);

        if (_driveStore is not null && result.Value.Existed)
            try { await _driveStore.DeleteAsync(request.NoteKey!, ct); } catch { /* silencioso */ }

        progress?.Report(new ProgressEvent("Nota deletada", 100, "done"));
        return RunResult<NotesResponse>.Success(new NotesResponse(request.Action) { DeleteResult = result.Value });
    }

    // ── Drive ─────────────────────────────────────────────────────────────────

    private async Task<(bool Skipped, string? Reason)> TrySyncToDriveAsync(
        string fileName, string localPath, string ext, CancellationToken ct)
    {
        if (_driveStore is null) return (true, "Google Drive não configurado.");
        try
        {
            var content = await File.ReadAllTextAsync(localPath, ct);
            var mimeType = ext == ".md" ? "text/markdown" : "text/plain";
            await _driveStore.UploadAsync(Path.GetFileName(fileName), content, mimeType, ct);
            return (false, null);
        }
        catch (Exception ex)
        {
            return (true, $"Falha no Drive (salvo localmente): {ex.Message}");
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string ResolveExtension(string? ext)
    {
        var n = ext?.Trim().ToLowerInvariant();
        return n == ".md" || n == ".txt" ? n : ".md";
    }

    private async Task<RunResult<bool>> SyncIndexAfterSimpleWriteAsync(
        string? rootPath, string noteKey, string content, CancellationToken ct)
    {
        try
        {
            var root = NotesPaths.ResolveRoot(rootPath);
            var fs = new SystemFileSystem();
            var indexStore = new NotesIndexStore(fs);
            var load = await indexStore.LoadAsync(root, ct);
            if (!load.IsSuccess || load.Value is null) return RunResult<bool>.Fail(load.Errors);

            var index = load.Value;
            var now = DateTime.UtcNow;
            var hash = NotesHash.Sha256Hex(content ?? string.Empty);
            var title = InferTitleFromFileName(noteKey);

            var entry = index.Items.FirstOrDefault(
                x => string.Equals(x.FileName, noteKey, StringComparison.OrdinalIgnoreCase));

            if (entry is null)
                index.Items.Add(new NotesIndexEntry
                {
                    Id = Guid.NewGuid().ToString("N"), Title = title, FileName = noteKey,
                    CreatedUtc = now, UpdatedUtc = now, Sha256 = hash
                });
            else
            {
                entry.UpdatedUtc = now; entry.Sha256 = hash;
                if (string.IsNullOrWhiteSpace(entry.Title)) entry.Title = title;
            }

            var save = await indexStore.SaveAsync(root, index, ct);
            if (!save.IsSuccess) return RunResult<bool>.Fail(save.Errors);
            return RunResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return RunResult<bool>.Fail(new ErrorDetail(
                "notes.simple.index.sync.failed", "Falha ao atualizar índice.", Exception: ex));
        }
    }

    private async Task<RunResult<NotesBackupReport>> MergeImportedAsync(
        string root, string tempDir, List<string> itemFiles, IProgressReporter? progress, CancellationToken ct)
    {
        try
        {
            var fs = new SystemFileSystem();
            var indexStore = new NotesIndexStore(fs);
            var currentIndexResult = await indexStore.LoadAsync(root, ct);
            if (!currentIndexResult.IsSuccess || currentIndexResult.Value is null)
                return RunResult<NotesBackupReport>.Fail(currentIndexResult.Errors);

            var index = currentIndexResult.Value;
            var rootItemsDir = NotesPaths.ItemsDir(root);
            fs.CreateDirectory(rootItemsDir);

            int imported = 0, skipped = 0;
            var conflicts = new List<string>();
            var total = Math.Max(1, itemFiles.Count);

            for (var i = 0; i < itemFiles.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                var src = itemFiles[i];
                var rel = Path.GetRelativePath(Path.Combine(tempDir, "items"), src).Replace('\\', '/');
                var dst = Path.Combine(rootItemsDir, rel.Replace('/', Path.DirectorySeparatorChar));
                var dstDir = Path.GetDirectoryName(dst);
                if (!string.IsNullOrWhiteSpace(dstDir)) fs.CreateDirectory(dstDir);

                var srcText = await File.ReadAllTextAsync(src, ct);
                var srcSha = NotesHash.Sha256Hex(srcText);

                if (!fs.FileExists(dst)) { fs.CopyFile(src, dst, true); imported++; }
                else
                {
                    var dstSha = NotesHash.Sha256Hex(await fs.ReadAllTextAsync(dst, ct));
                    if (string.Equals(srcSha, dstSha, StringComparison.OrdinalIgnoreCase)) skipped++;
                    else { fs.CopyFile(src, BuildConflictPath(dst), true); conflicts.Add(Path.GetFileName(dst)); }
                }

                progress?.Report(new ProgressEvent("Importando notas",
                    (int)Math.Round(((i + 1) / (double)total) * 80) + 10, "merge"));

                var existing = index.Items.FirstOrDefault(
                    x => string.Equals(x.FileName, rel, StringComparison.OrdinalIgnoreCase));
                if (existing is null)
                    index.Items.Add(new NotesIndexEntry
                    {
                        Id = Guid.NewGuid().ToString("N"), Title = InferTitleFromFileName(rel),
                        FileName = rel, CreatedUtc = DateTime.UtcNow, UpdatedUtc = DateTime.UtcNow, Sha256 = srcSha
                    });
                else { existing.UpdatedUtc = DateTime.UtcNow; existing.Sha256 = srcSha; }
            }

            var save = await indexStore.SaveAsync(root, index, ct);
            if (!save.IsSuccess) return RunResult<NotesBackupReport>.Fail(save.Errors);
            return RunResult<NotesBackupReport>.Success(new NotesBackupReport(imported, skipped, conflicts.Count, conflicts));
        }
        catch (Exception ex)
        {
            return RunResult<NotesBackupReport>.Fail(new ErrorDetail(
                "notes.backup.import.merge.failed", "Falha ao mesclar notas importadas.", Exception: ex));
        }
    }

    private static string BuildConflictPath(string existingPath)
    {
        var dir = Path.GetDirectoryName(existingPath) ?? string.Empty;
        var name = Path.GetFileNameWithoutExtension(existingPath);
        var ext = Path.GetExtension(existingPath);
        return Path.Combine(dir, $"{name} (conflict {DateTime.Now:yyyyMMdd-HHmmss}){ext}");
    }

    private static string InferTitleFromFileName(string rel)
    {
        var file = Path.GetFileNameWithoutExtension(rel);
        var parts = file.Split(" - ", 2, StringSplitOptions.TrimEntries);
        return parts.Length == 2 ? parts[1] : file;
    }

    private static RunSummary BuildSummary(
        NotesRequest request, RunResult<NotesResponse> result, TimeSpan duration)
    {
        int processed = 0, changed = 0, ignored = 0;
        string? output = null;

        if (result.IsSuccess && result.Value is not null)
        {
            var val = result.Value;
            if      (val.ListResult     != null) processed = val.ListResult.Items.Count;
            else if (val.ReadResult     != null) { processed = 1; output = val.ReadResult.Path; }
            else if (val.WriteResult    != null) { processed = 1; changed = 1; output = val.WriteResult.Path; }
            else if (val.CreateResult   != null) { processed = 1; changed = 1; output = val.CreateResult.Path; }
            else if (val.DeleteResult   != null) { processed = 1; changed = val.DeleteResult.Existed ? 1 : 0; output = val.DeleteResult.Path; }
            else if (val.ExportedZipPath != null) { processed = 1; changed = 1; output = val.ExportedZipPath; }
            else if (val.BackupReport   != null)
            {
                processed = val.BackupReport.ImportedCount + val.BackupReport.SkippedCount + val.BackupReport.ConflictCount;
                changed = val.BackupReport.ImportedCount + val.BackupReport.ConflictCount;
                ignored = val.BackupReport.SkippedCount;
            }
        }

        return new RunSummary(
            ToolName: "Notes",
            Mode: request.Action.ToString(),
            MainInput: request.NoteKey ?? request.ZipPath ?? request.NotesRootPath ?? "Default",
            OutputLocation: output,
            Processed: processed, Changed: changed, Ignored: ignored,
            Failed: result.Errors.Count, Duration: duration);
    }
}
