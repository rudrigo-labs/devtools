using DevTools.Core.Abstractions;
using DevTools.Notes.Models;

namespace DevTools.Notes.Repositories;

public interface INotesEntityRepository : IRepository<NotesEntity>
{
    Task<NotesEntity?> GetDefaultAsync(CancellationToken ct = default);
    Task SetDefaultAsync(string id, CancellationToken ct = default);
}
