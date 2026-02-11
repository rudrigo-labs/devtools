using System.Text;
using DevTools.Core.Abstractions;
using DevTools.Core.Results;
using DevTools.Notes.Abstractions;
using DevTools.Notes.Models;
using DevTools.Core.Providers;

namespace DevTools.Notes.Providers;

public sealed class SystemNotesStore : INotesStore
{
    private readonly IFileSystem _fs;

    public string RootPath { get; }

    public SystemNotesStore(string rootPath, IFileSystem? fileSystem = null)
    {
        _fs = fileSystem ?? new SystemFileSystem();
        RootPath = ResolveRootPath(rootPath);
        _fs.CreateDirectory(RootPath);
    }

    public async Task<RunResult<NoteReadResult>> ReadAsync(string key, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            return RunResult<NoteReadResult>.Fail(new ErrorDetail("notes.key.required", "Note key is required."));

        var path = ResolveNotePath(key);

        try
        {
            if (!_fs.FileExists(path))
                return RunResult<NoteReadResult>.Success(new NoteReadResult(key, path, null, false));

            var content = await _fs.ReadAllTextAsync(path, ct).ConfigureAwait(false);
            return RunResult<NoteReadResult>.Success(new NoteReadResult(key, path, content, true));
        }
        catch (Exception ex)
        {
            return RunResult<NoteReadResult>.Fail(new ErrorDetail(
                "notes.read.failed",
                "Failed to read note.",
                ex.Message,
                ex));
        }
    }

    public async Task<RunResult<NoteWriteResult>> WriteAsync(string key, string content, bool overwrite = true, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            return RunResult<NoteWriteResult>.Fail(new ErrorDetail("notes.key.required", "Note key is required."));

        if (content is null)
            return RunResult<NoteWriteResult>.Fail(new ErrorDetail("notes.content.required", "Note content is required."));

        var path = ResolveNotePath(key);
        var exists = _fs.FileExists(path);

        if (exists && !overwrite)
            return RunResult<NoteWriteResult>.Fail(new ErrorDetail("notes.write.exists", "Note already exists.", path));

        try
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir))
                _fs.CreateDirectory(dir);

            await _fs.WriteAllTextAsync(path, content, ct).ConfigureAwait(false);
            var bytes = Encoding.UTF8.GetByteCount(content);
            return RunResult<NoteWriteResult>.Success(new NoteWriteResult(key, path, bytes, exists));
        }
        catch (Exception ex)
        {
            return RunResult<NoteWriteResult>.Fail(new ErrorDetail(
                "notes.write.failed",
                "Failed to write note.",
                ex.Message,
                ex));
        }
    }

    private string ResolveNotePath(string key)
    {
        var safeKey = SanitizeKey(key);
        return Path.Combine(RootPath, $"{safeKey}.txt");
    }

    private static string ResolveRootPath(string rootPath)
    {
        if (!string.IsNullOrWhiteSpace(rootPath))
            return Path.GetFullPath(rootPath);

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var folder = Path.Combine(appData, "DevTools", "notes");
        return folder;
    }

    private static string SanitizeKey(string key)
    {
        var trimmed = key.Trim();
        if (trimmed.Length == 0)
            return "note";

        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(trimmed.Length);

        foreach (var ch in trimmed)
            sb.Append(invalid.Contains(ch) ? '_' : ch);

        return sb.Length == 0 ? "note" : sb.ToString();
    }
}
