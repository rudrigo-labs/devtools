using DevTools.Core.Results;
using DevTools.Core.Validation;
using DevTools.Ngrok.Engine;
using DevTools.Ngrok.Models;
using DevTools.Ngrok.Services;

namespace DevTools.Host.Wpf.Facades;

public interface INgrokFacade
{
    Task<IReadOnlyList<NgrokEntity>> LoadAsync(CancellationToken ct = default);
    Task<ValidationResult> SaveAsync(NgrokEntity entity, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
    Task<RunResult<NgrokResult>> ExecuteAsync(NgrokRequest request, CancellationToken ct = default);
}

public sealed class NgrokFacade : INgrokFacade
{
    private readonly NgrokEntityService _entityService;
    private readonly NgrokEngine _engine;

    public NgrokFacade(NgrokEntityService entityService, NgrokEngine engine)
    {
        _entityService = entityService;
        _engine        = engine;
    }

    public Task<IReadOnlyList<NgrokEntity>> LoadAsync(CancellationToken ct = default) =>
        _entityService.ListAsync(ct);

    public Task<ValidationResult> SaveAsync(NgrokEntity entity, CancellationToken ct = default) =>
        _entityService.UpsertAsync(entity, ct);

    public Task DeleteAsync(string id, CancellationToken ct = default) =>
        _entityService.DeleteAsync(id, ct);

    public Task<RunResult<NgrokResult>> ExecuteAsync(NgrokRequest request, CancellationToken ct = default) =>
        _engine.ExecuteAsync(request, cancellationToken: ct);
}
