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
    Task<string> ResolveBaseUrlAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TunnelInfo>> GetActiveTunnelsAsync(string? baseUrl = null, CancellationToken ct = default);
    Task<bool> CloseTunnelAsync(string tunnelName, string? baseUrl = null, CancellationToken ct = default);
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

    public async Task<string> ResolveBaseUrlAsync(CancellationToken ct = default)
    {
        var entities = await LoadAsync(ct).ConfigureAwait(false);
        var selected = entities.FirstOrDefault(x => x.IsDefault)
            ?? entities.FirstOrDefault();

        var baseUrl = selected?.BaseUrl?.Trim();
        return string.IsNullOrWhiteSpace(baseUrl)
            ? "http://127.0.0.1:4040/"
            : baseUrl;
    }

    public async Task<IReadOnlyList<TunnelInfo>> GetActiveTunnelsAsync(string? baseUrl = null, CancellationToken ct = default)
    {
        var effectiveBaseUrl = string.IsNullOrWhiteSpace(baseUrl)
            ? await ResolveBaseUrlAsync(ct).ConfigureAwait(false)
            : baseUrl.Trim();

        var result = await ExecuteAsync(new NgrokRequest
        {
            Action = NgrokAction.ListTunnels,
            BaseUrl = effectiveBaseUrl
        }, ct).ConfigureAwait(false);

        return result.IsSuccess
            ? result.Value?.Tunnels ?? Array.Empty<TunnelInfo>()
            : Array.Empty<TunnelInfo>();
    }

    public async Task<bool> CloseTunnelAsync(string tunnelName, string? baseUrl = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(tunnelName))
            return false;

        var effectiveBaseUrl = string.IsNullOrWhiteSpace(baseUrl)
            ? await ResolveBaseUrlAsync(ct).ConfigureAwait(false)
            : baseUrl.Trim();

        var result = await ExecuteAsync(new NgrokRequest
        {
            Action = NgrokAction.CloseTunnel,
            BaseUrl = effectiveBaseUrl,
            TunnelName = tunnelName
        }, ct).ConfigureAwait(false);

        return result.IsSuccess && (result.Value?.Closed ?? false);
    }
}
