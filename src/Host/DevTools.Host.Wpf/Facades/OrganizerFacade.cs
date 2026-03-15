using DevTools.Core.Results;
using DevTools.Core.Validation;
using DevTools.Organizer.Engine;
using DevTools.Organizer.Models;
using DevTools.Organizer.Services;

namespace DevTools.Host.Wpf.Facades;

public interface IOrganizerFacade
{
    Task<IReadOnlyList<OrganizerEntity>> LoadAsync(CancellationToken ct = default);
    Task<ValidationResult> SaveAsync(OrganizerEntity entity, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
    Task<RunResult<OrganizerResult>> ExecuteAsync(OrganizerRequest request, CancellationToken ct = default);
}

public sealed class OrganizerFacade : IOrganizerFacade
{
    private readonly OrganizerEntityService _entityService;
    private readonly OrganizerEngine _engine;

    public OrganizerFacade(OrganizerEntityService entityService, OrganizerEngine engine)
    {
        _entityService = entityService;
        _engine = engine;
    }

    public Task<IReadOnlyList<OrganizerEntity>> LoadAsync(CancellationToken ct = default) =>
        _entityService.ListAsync(ct);

    public Task<ValidationResult> SaveAsync(OrganizerEntity entity, CancellationToken ct = default) =>
        _entityService.UpsertAsync(entity, ct);

    public Task DeleteAsync(string id, CancellationToken ct = default) =>
        _entityService.DeleteAsync(id, ct);

    public Task<RunResult<OrganizerResult>> ExecuteAsync(OrganizerRequest request, CancellationToken ct = default) =>
        _engine.ExecuteAsync(request, cancellationToken: ct);
}
