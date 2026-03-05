using DevTools.Core.Results;
using DevTools.Ngrok.Models;
using DevTools.Ngrok.Services;

namespace DevTools.Ngrok.Engine;

public sealed class NgrokSetupService
{
    private readonly NgrokConfigEngine _configEngine;
    private readonly NgrokTunnelEngine _tunnelEngine;

    public NgrokSetupService(
        NgrokConfigEngine? configEngine = null,
        NgrokTunnelEngine? tunnelEngine = null)
    {
        _configEngine = configEngine ?? new NgrokConfigEngine();
        _tunnelEngine = tunnelEngine ?? new NgrokTunnelEngine(_configEngine);
    }

    public bool IsConfigured()
    {
        return _configEngine.IsConfigured();
    }

    public NgrokSettings GetSettings()
    {
        return _configEngine.GetSettings();
    }

    public void SaveAuthtoken(string token)
    {
        _configEngine.SaveAuthtoken(token);
    }

    public void SaveSettings(NgrokSettings settings)
    {
        _configEngine.SaveSettings(settings);
    }

    public Task<RunResult<NgrokResponse>> ListTunnelsAsync(CancellationToken ct = default)
    {
        return _tunnelEngine.ListTunnelsAsync(ct);
    }

    public Task<RunResult<NgrokResponse>> ListTunnels(CancellationToken ct = default)
    {
        return _tunnelEngine.ListTunnels(ct);
    }

    public Task<RunResult<NgrokResponse>> StartTunnelAsync(int port, CancellationToken ct = default)
    {
        return _tunnelEngine.StartTunnelAsync(port, ct);
    }

    public Task<RunResult<NgrokResponse>> StartTunnel(int port, CancellationToken ct = default)
    {
        return _tunnelEngine.StartTunnel(port, ct);
    }

    public Task<RunResult<NgrokResponse>> StopTunnelAsync(CancellationToken ct = default)
    {
        return _tunnelEngine.StopTunnelAsync(ct);
    }

    public Task<RunResult<NgrokResponse>> StopTunnel(CancellationToken ct = default)
    {
        return _tunnelEngine.StopTunnel(ct);
    }
}
