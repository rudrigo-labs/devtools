using DevTools.Core.Results;
using DevTools.Core.Validation;
using DevTools.Snapshot.Models;

namespace DevTools.Host.Wpf.Facades;

public interface ISnapshotFacade
{
    Task<IReadOnlyList<SnapshotEntity>> LoadAsync(CancellationToken ct = default);
    Task<ValidationResult> SaveAsync(SnapshotEntity entity, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
    Task<RunResult<SnapshotResult>> ExecuteAsync(SnapshotRequest request, CancellationToken ct = default);
}
