using DevTools.Core.Results;
using DevTools.Organizer.Engine;
using DevTools.Organizer.Models;

namespace DevTools.Host.Wpf.Facades;

public interface IOrganizerFacade
{
    Task<RunResult<OrganizerResult>> ExecuteAsync(OrganizerRequest request, CancellationToken ct = default);
}

public sealed class OrganizerFacade : IOrganizerFacade
{
    private readonly OrganizerEngine _engine;

    public OrganizerFacade(OrganizerEngine engine)
    {
        _engine = engine;
    }

    public Task<RunResult<OrganizerResult>> ExecuteAsync(OrganizerRequest request, CancellationToken ct = default) =>
        _engine.ExecuteAsync(request, cancellationToken: ct);
}
