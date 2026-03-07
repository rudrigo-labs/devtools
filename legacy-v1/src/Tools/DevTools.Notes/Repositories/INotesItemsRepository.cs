using DevTools.Core.Results;
using DevTools.Notes.Models;

namespace DevTools.Notes.Repositories;

public interface INotesItemsRepository
{
    Task<RunResult<NoteCreateResult>> CreateAsync(
        string? rootPath,
        string title,
        string content,
        DateTime? localDate,
        bool useMarkdown,
        bool createDateFolder,
        CancellationToken ct = default);

    Task<RunResult<NoteListResult>> ListAsync(string? rootPath, CancellationToken ct = default);

    Task<RunResult<NoteReadResult>> ReadAsync(string? rootPath, string fileName, CancellationToken ct = default);

    Task<RunResult<NoteWriteResult>> WriteAsync(
        string? rootPath,
        string fileName,
        string content,
        bool overwrite = true,
        CancellationToken ct = default);

    Task<RunResult<NoteDeleteResult>> DeleteAsync(string? rootPath, string fileName, CancellationToken ct = default);
}
