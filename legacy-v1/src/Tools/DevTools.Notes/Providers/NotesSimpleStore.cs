using System.Text;
using DevTools.Core.Abstractions;
using DevTools.Core.Providers;
using DevTools.Core.Results;
using DevTools.Notes.Models;

namespace DevTools.Notes.Providers;

public sealed class NotesSimpleStore
{
    private readonly IFileSystem _fs;
    private readonly NotesIndexStore _indexStore;

    public NotesSimpleStore(IFileSystem? fileSystem = null)
    {
        _fs = fileSystem ?? new SystemFileSystem();
        _indexStore = new NotesIndexStore(_fs);
    }

    public async Task<RunResult<NoteCreateResult>> CreateAsync(
        string? rootPath,
        string title,
        string content,
        DateTime? localDate,
        bool useMarkdown,
        bool createDateFolder,
        CancellationToken ct = default)
    {
        var root = NotesPaths.ResolveRoot(rootPath);
        var itemsRoot = NotesPaths.ItemsDir(root);
        _fs.CreateDirectory(itemsRoot);

        var nowUtc = DateTime.UtcNow;
        var date = (localDate ?? DateTime.Now).Date;
        var slug = NotesSlug.ToSlug(title);
        var ext = useMarkdown ? ".md" : ".txt";

        var relativeDir = createDateFolder ? Path.Combine(date.ToString("yyyy-MM-dd")) : string.Empty;
        var dir = string.IsNullOrWhiteSpace(relativeDir) ? itemsRoot : Path.Combine(itemsRoot, relativeDir);
        _fs.CreateDirectory(dir);

        var baseFile = $"{date:yyyy-MM-dd} - {slug}{ext}";
        var fileName = EnsureUniqueFileName(dir, baseFile);
        var path = Path.Combine(dir, fileName);

        var normalized = NormalizeNoteContent(title, content, useMarkdown);
        var sha = NotesHash.Sha256Hex(normalized);

        try
        {
            await _fs.WriteAllTextAsync(path, normalized, ct).ConfigureAwait(false);

            var loadIndex = await _indexStore.LoadAsync(root, ct).ConfigureAwait(false);
            if (!loadIndex.IsSuccess || loadIndex.Value is null)
                return RunResult<NoteCreateResult>.Fail(loadIndex.Errors);

            var index = loadIndex.Value;
            var id = Guid.NewGuid().ToString("N");
            var relPath = Path.GetRelativePath(itemsRoot, path).Replace('\\', '/');

            index.Items.Add(new NotesIndexEntry
            {
                Id = id,
                Title = title,
                FileName = relPath,
                CreatedUtc = nowUtc,
                UpdatedUtc = nowUtc,
                Sha256 = sha,
                Tags = null
            });

            var saveIndex = await _indexStore.SaveAsync(root, index, ct).ConfigureAwait(false);
            if (!saveIndex.IsSuccess)
                return RunResult<NoteCreateResult>.Fail(saveIndex.Errors);

            return RunResult<NoteCreateResult>.Success(new NoteCreateResult(
                id,
                title,
                relPath,
                path,
                sha,
                nowUtc,
                nowUtc));
        }
        catch (Exception ex)
        {
            return RunResult<NoteCreateResult>.Fail(new ErrorDetail(
                "notes.simple.create.failed",
                "Failed to create note.",
                Cause: path,
                Exception: ex));
        }
    }

    public async Task<RunResult<NoteListResult>> ListAsync(string? rootPath, CancellationToken ct = default)
    {
        var root = NotesPaths.ResolveRoot(rootPath);
        var load = await _indexStore.LoadAsync(root, ct).ConfigureAwait(false);
        if (!load.IsSuccess || load.Value is null)
            return RunResult<NoteListResult>.Fail(load.Errors);

        var items = load.Value.Items
            .OrderByDescending(x => x.UpdatedUtc)
            .Select(x => new NoteListItem(
                x.Id,
                x.Title,
                x.FileName,
                x.CreatedUtc,
                x.UpdatedUtc,
                x.Tags))
            .ToList();

        return RunResult<NoteListResult>.Success(new NoteListResult(items));
    }

    public async Task<RunResult<NoteReadResult>> ReadItemAsync(
        string? rootPath,
        string fileName,
        CancellationToken ct = default)
    {
        var root = NotesPaths.ResolveRoot(rootPath);
        var resolved = ResolveItemPath(root, fileName);
        if (!resolved.IsSuccess || resolved.Value is null)
            return RunResult<NoteReadResult>.Fail(resolved.Errors);

        var path = resolved.Value;
        try
        {
            if (!_fs.FileExists(path))
                return RunResult<NoteReadResult>.Success(new NoteReadResult(fileName, path, null, false));

            var content = await _fs.ReadAllTextAsync(path, ct).ConfigureAwait(false);
            return RunResult<NoteReadResult>.Success(new NoteReadResult(fileName, path, content, true));
        }
        catch (Exception ex)
        {
            return RunResult<NoteReadResult>.Fail(new ErrorDetail(
                "notes.simple.read.failed",
                "Failed to read note item.",
                Cause: path,
                Exception: ex));
        }
    }

    public async Task<RunResult<NoteWriteResult>> WriteItemAsync(
        string? rootPath,
        string fileName,
        string content,
        bool overwrite = true,
        CancellationToken ct = default)
    {
        var root = NotesPaths.ResolveRoot(rootPath);
        var resolved = ResolveItemPath(root, fileName);
        if (!resolved.IsSuccess || resolved.Value is null)
            return RunResult<NoteWriteResult>.Fail(resolved.Errors);

        var path = resolved.Value;
        var exists = _fs.FileExists(path);

        if (exists && !overwrite)
            return RunResult<NoteWriteResult>.Fail(new ErrorDetail("notes.simple.write.exists", "Note already exists.", path));

        try
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir))
                _fs.CreateDirectory(dir);

            await _fs.WriteAllTextAsync(path, content ?? string.Empty, ct).ConfigureAwait(false);
            var bytes = Encoding.UTF8.GetByteCount(content ?? string.Empty);
            return RunResult<NoteWriteResult>.Success(new NoteWriteResult(fileName, path, bytes, exists));
        }
        catch (Exception ex)
        {
            return RunResult<NoteWriteResult>.Fail(new ErrorDetail(
                "notes.simple.write.failed",
                "Failed to write note item.",
                Cause: path,
                Exception: ex));
        }
    }

    public async Task<RunResult<NoteDeleteResult>> DeleteItemAsync(
        string? rootPath,
        string fileName,
        CancellationToken ct = default)
    {
        var root = NotesPaths.ResolveRoot(rootPath);
        var resolved = ResolveItemPath(root, fileName);
        if (!resolved.IsSuccess || resolved.Value is null)
            return RunResult<NoteDeleteResult>.Fail(resolved.Errors);

        var path = resolved.Value;
        var existed = _fs.FileExists(path);

        try
        {
            if (existed)
            {
                _fs.DeleteFile(path);
            }

            var load = await _indexStore.LoadAsync(root, ct).ConfigureAwait(false);
            if (!load.IsSuccess || load.Value is null)
                return RunResult<NoteDeleteResult>.Fail(load.Errors);

            var index = load.Value;
            index.Items.RemoveAll(x => string.Equals(x.FileName, fileName, StringComparison.OrdinalIgnoreCase));

            var save = await _indexStore.SaveAsync(root, index, ct).ConfigureAwait(false);
            if (!save.IsSuccess)
                return RunResult<NoteDeleteResult>.Fail(save.Errors);

            return RunResult<NoteDeleteResult>.Success(new NoteDeleteResult(fileName, path, existed));
        }
        catch (Exception ex)
        {
            return RunResult<NoteDeleteResult>.Fail(new ErrorDetail(
                "notes.simple.delete.failed",
                "Failed to delete note item.",
                Cause: path,
                Exception: ex));
        }
    }

    private string EnsureUniqueFileName(string dir, string fileName)
    {
        var candidate = fileName;
        var name = Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.GetExtension(fileName);

        var i = 1;
        while (_fs.FileExists(Path.Combine(dir, candidate)))
        {
            i++;
            candidate = $"{name} ({i}){ext}";
        }

        return candidate;
    }

    private RunResult<string> ResolveItemPath(string root, string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return RunResult<string>.Fail(new ErrorDetail("notes.simple.key.required", "Note file name is required."));

        var itemsRoot = Path.GetFullPath(NotesPaths.ItemsDir(root));
        _fs.CreateDirectory(itemsRoot);

        var normalized = fileName
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar)
            .TrimStart(Path.DirectorySeparatorChar);

        var fullPath = Path.GetFullPath(Path.Combine(itemsRoot, normalized));
        var rootWithSeparator = itemsRoot.EndsWith(Path.DirectorySeparatorChar)
            ? itemsRoot
            : itemsRoot + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(fullPath, itemsRoot, StringComparison.OrdinalIgnoreCase))
        {
            return RunResult<string>.Fail(new ErrorDetail("notes.simple.key.invalid", "Invalid note file path."));
        }

        return RunResult<string>.Success(fullPath);
    }

    private static string NormalizeNoteContent(string title, string content, bool useMarkdown)
    {
        var sb = new StringBuilder();
        if (useMarkdown)
        {
            sb.Append("# ");
            sb.AppendLine(title.Trim());
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine(title.Trim());
            sb.AppendLine(new string('-', Math.Max(3, title.Trim().Length)));
        }

        sb.Append(content ?? string.Empty);
        if (!sb.ToString().EndsWith("\n", StringComparison.Ordinal))
            sb.AppendLine();
        return sb.ToString();
    }
}
