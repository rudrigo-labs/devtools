using System.Text.Json;
using DevTools.Core.Abstractions;
using DevTools.Core.Providers;
using DevTools.Core.Results;
using DevTools.Notes.Models;

namespace DevTools.Notes.Providers;

public sealed class NotesIndexStore
{
    private readonly IFileSystem _fs;

    public NotesIndexStore(IFileSystem? fileSystem = null)
    {
        _fs = fileSystem ?? new SystemFileSystem();
    }

    public async Task<RunResult<NotesIndex>> LoadAsync(string root, CancellationToken ct = default)
    {
        var path = NotesPaths.IndexPath(root);
        try
        {
            if (!_fs.FileExists(path))
                return RunResult<NotesIndex>.Success(new NotesIndex());

            var json = await _fs.ReadAllTextAsync(path, ct).ConfigureAwait(false);
            var index = JsonSerializer.Deserialize<NotesIndex>(json, JsonOptions()) ?? new NotesIndex();
            index.Items ??= new List<NotesIndexEntry>();
            return RunResult<NotesIndex>.Success(index);
        }
        catch (Exception ex)
        {
            return RunResult<NotesIndex>.Fail(new ErrorDetail(
                "notes.index.load.failed",
                "Failed to load index.",
                Cause: path,
                Exception: ex));
        }
    }

    public async Task<RunResult<bool>> SaveAsync(string root, NotesIndex index, CancellationToken ct = default)
    {
        var path = NotesPaths.IndexPath(root);
        try
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir))
                _fs.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(index, JsonOptions());
            await _fs.WriteAllTextAsync(path, json, ct).ConfigureAwait(false);
            return RunResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return RunResult<bool>.Fail(new ErrorDetail(
                "notes.index.save.failed",
                "Failed to save index.",
                Cause: path,
                Exception: ex));
        }
    }

    private static JsonSerializerOptions JsonOptions() => new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
