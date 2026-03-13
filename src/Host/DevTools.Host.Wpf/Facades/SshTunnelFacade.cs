using DevTools.Core.Results;
using DevTools.Core.Validation;
using DevTools.SSHTunnel.Engine;
using DevTools.SSHTunnel.Models;
using DevTools.SSHTunnel.Services;

namespace DevTools.Host.Wpf.Facades;

public interface ISshTunnelFacade
{
    Task<IReadOnlyList<SshTunnelEntity>> LoadAsync(CancellationToken ct = default);
    Task<ValidationResult> SaveAsync(SshTunnelEntity entity, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
    Task<RunResult<SshTunnelResult>> ExecuteAsync(SshTunnelRequest request, CancellationToken ct = default);
    TunnelState CurrentState { get; }
    bool IsRunning { get; }
}

public sealed class SshTunnelFacade : ISshTunnelFacade
{
    private readonly SshTunnelEntityService _entityService;
    private readonly SshTunnelEngine _engine;

    public SshTunnelFacade(SshTunnelEntityService entityService, SshTunnelEngine engine)
    {
        _entityService = entityService;
        _engine        = engine;
    }

    public Task<IReadOnlyList<SshTunnelEntity>> LoadAsync(CancellationToken ct = default) =>
        _entityService.ListAsync(ct);

    public Task<ValidationResult> SaveAsync(SshTunnelEntity entity, CancellationToken ct = default) =>
        _entityService.UpsertAsync(entity, ct);

    public Task DeleteAsync(string id, CancellationToken ct = default) =>
        _entityService.DeleteAsync(id, ct);

    public Task<RunResult<SshTunnelResult>> ExecuteAsync(SshTunnelRequest request, CancellationToken ct = default) =>
        _engine.ExecuteAsync(request, cancellationToken: ct);

    public TunnelState CurrentState => _engine.CurrentState;
    public bool IsRunning => _engine.IsRunning;
}
