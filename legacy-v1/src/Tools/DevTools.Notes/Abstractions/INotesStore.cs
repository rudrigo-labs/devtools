using DevTools.Core.Results;
using DevTools.Notes.Models;

namespace DevTools.Notes.Abstractions;

public interface INotesStore
{
    Task<RunResult<NoteReadResult>> ReadAsync(string key, CancellationToken ct = default);
    Task<RunResult<NoteWriteResult>> WriteAsync(string key, string content, bool overwrite = true, CancellationToken ct = default);
    string RootPath { get; }
}
