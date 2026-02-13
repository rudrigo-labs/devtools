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
