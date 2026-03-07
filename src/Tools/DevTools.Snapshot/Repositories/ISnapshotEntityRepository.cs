using DevTools.Core.Abstractions;
using DevTools.Snapshot.Models;

namespace DevTools.Snapshot.Repositories;

public interface ISnapshotEntityRepository : IRepository<SnapshotEntity>
{
    Task<SnapshotEntity?> GetDefaultAsync(CancellationToken ct = default);
    Task SetDefaultAsync(string id, CancellationToken ct = default);
}

