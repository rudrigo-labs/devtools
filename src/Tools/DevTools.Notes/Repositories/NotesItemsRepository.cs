using DevTools.Core.Abstractions;
using DevTools.Core.Results;
using DevTools.Notes.Models;
using DevTools.Notes.Providers;

namespace DevTools.Notes.Repositories;

public sealed class NotesItemsRepository : INotesItemsRepository
{
    private readonly NotesSimpleStore _store;

    public NotesItemsRepository(IFileSystem? fileSystem = null)
    {
        _store = new NotesSimpleStore(fileSystem);
    }

    public Task<RunResult<NoteCreateResult>> CreateAsync(
        string? rootPath,
        string title,
        string content,
        DateTime? localDate,
        bool useMarkdown,
        bool createDateFolder,
        CancellationToken ct = default)
    {
        return _store.CreateAsync(rootPath, title, content, localDate, useMarkdown, createDateFolder, ct);
    }

    public Task<RunResult<NoteListResult>> ListAsync(string? rootPath, CancellationToken ct = default)
    {
        return _store.ListAsync(rootPath, ct);
    }

    public Task<RunResult<NoteReadResult>> ReadAsync(string? rootPath, string fileName, CancellationToken ct = default)
    {
        return _store.ReadItemAsync(rootPath, fileName, ct);
    }

    public Task<RunResult<NoteWriteResult>> WriteAsync(
        string? rootPath,
        string fileName,
        string content,
        bool overwrite = true,
        CancellationToken ct = default)
    {
        return _store.WriteItemAsync(rootPath, fileName, content, overwrite, ct);
    }

    public Task<RunResult<NoteDeleteResult>> DeleteAsync(string? rootPath, string fileName, CancellationToken ct = default)
    {
        return _store.DeleteItemAsync(rootPath, fileName, ct);
    }
}
