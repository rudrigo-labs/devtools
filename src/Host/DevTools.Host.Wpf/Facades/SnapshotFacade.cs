using DevTools.Core.Results;
using DevTools.Core.Validation;
using DevTools.Snapshot.Engine;
using DevTools.Snapshot.Models;
using DevTools.Snapshot.Services;

namespace DevTools.Host.Wpf.Facades;

public sealed class SnapshotFacade : ISnapshotFacade
{
    private readonly SnapshotEntityService _entityService;
    private readonly SnapshotEngine _engine;

    public SnapshotFacade(SnapshotEntityService entityService, SnapshotEngine engine)
    {
        _entityService = entityService;
        _engine = engine;
    }

    public Task<IReadOnlyList<SnapshotEntity>> LoadAsync(CancellationToken ct = default) =>
        _entityService.ListAsync(ct);

    public Task<ValidationResult> SaveAsync(SnapshotEntity entity, CancellationToken ct = default) =>
        _entityService.UpsertAsync(entity, ct);

    public Task DeleteAsync(string id, CancellationToken ct = default) =>
        _entityService.DeleteAsync(id, ct);

    public Task<RunResult<SnapshotResult>> ExecuteAsync(SnapshotRequest request, CancellationToken ct = default) =>
        _engine.ExecuteAsync(request, cancellationToken: ct);
}
