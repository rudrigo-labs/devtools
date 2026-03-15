using DevTools.Core.Abstractions;
using DevTools.Organizer.Models;

namespace DevTools.Organizer.Repositories;

public interface IOrganizerEntityRepository : IRepository<OrganizerEntity>
{
    Task<OrganizerEntity?> GetDefaultAsync(CancellationToken ct = default);
    Task SetDefaultAsync(string id, CancellationToken ct = default);
}
