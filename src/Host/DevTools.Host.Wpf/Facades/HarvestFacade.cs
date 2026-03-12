using DevTools.Core.Results;
using DevTools.Core.Validation;
using DevTools.Harvest.Engine;
using DevTools.Harvest.Models;
using DevTools.Harvest.Services;

namespace DevTools.Host.Wpf.Facades;

public sealed class HarvestFacade : IHarvestFacade
{
    private readonly HarvestEntityService _entityService;
    private readonly HarvestEngine _engine;

    public HarvestFacade(HarvestEntityService entityService, HarvestEngine engine)
    {
        _entityService = entityService;
        _engine = engine;
    }

    public Task<IReadOnlyList<HarvestEntity>> LoadAsync(CancellationToken ct = default) =>
        _entityService.ListAsync(ct);

    public Task<ValidationResult> SaveAsync(HarvestEntity entity, CancellationToken ct = default) =>
        _entityService.UpsertAsync(entity, ct);

    public Task DeleteAsync(string id, CancellationToken ct = default) =>
        _entityService.DeleteAsync(id, ct);

    public Task<RunResult<HarvestResult>> ExecuteAsync(HarvestRequest request, CancellationToken ct = default) =>
        _engine.ExecuteAsync(request, cancellationToken: ct);
}
