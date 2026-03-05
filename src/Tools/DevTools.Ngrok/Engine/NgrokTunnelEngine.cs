using System.Text.RegularExpressions;
using DevTools.Core.Results;
using DevTools.Ngrok.Models;

namespace DevTools.Ngrok.Engine;

public sealed class NgrokTunnelEngine
{
    private readonly NgrokConfigEngine _configEngine;
    private readonly NgrokEngine _ngrokEngine;

    public NgrokTunnelEngine(NgrokConfigEngine configEngine, NgrokEngine? ngrokEngine = null)
    {
        _configEngine = configEngine;
        _ngrokEngine = ngrokEngine ?? new NgrokEngine();
    }

    public Task<RunResult<NgrokResponse>> ListTunnelsAsync(CancellationToken ct = default)
    {
        var request = new NgrokRequest(NgrokAction.ListTunnels);
        return _ngrokEngine.ExecuteAsync(request, ct: ct);
    }

    public Task<RunResult<NgrokResponse>> ListTunnels(CancellationToken ct = default)
    {
        return ListTunnelsAsync(ct);
    }

    public Task<RunResult<NgrokResponse>> StartTunnelAsync(int port, CancellationToken ct = default)
    {
        if (port < 1 || port > 65535)
            return Task.FromResult(RunResult<NgrokResponse>.Fail(new ErrorDetail("ngrok.start.port.invalid", "Port must be between 1 and 65535.")));

        var settings = _configEngine.GetSettings();
        if (string.IsNullOrWhiteSpace(settings.AuthToken))
            return Task.FromResult(RunResult<NgrokResponse>.Fail(new ErrorDetail("ngrok.authtoken.required", "Authtoken is required before starting tunnel.")));

        var args = ParseAdditionalArgs(settings.AdditionalArgs).ToList();
        if (!ContainsAuthTokenFlag(args))
        {
            args.Add("--authtoken");
            args.Add(settings.AuthToken.Trim());
        }

        var request = new NgrokRequest(
            NgrokAction.StartHttp,
            StartOptions: new NgrokStartOptions(
                "http",
                port,
                string.IsNullOrWhiteSpace(settings.ExecutablePath) ? null : settings.ExecutablePath.Trim(),
                args));

        return _ngrokEngine.ExecuteAsync(request, ct: ct);
    }

    public Task<RunResult<NgrokResponse>> StartTunnel(int port, CancellationToken ct = default)
    {
        return StartTunnelAsync(port, ct);
    }

    public Task<RunResult<NgrokResponse>> StopTunnelAsync(CancellationToken ct = default)
    {
        var request = new NgrokRequest(NgrokAction.KillAll);
        return _ngrokEngine.ExecuteAsync(request, ct: ct);
    }

    public Task<RunResult<NgrokResponse>> StopTunnel(CancellationToken ct = default)
    {
        return StopTunnelAsync(ct);
    }

    private static IReadOnlyList<string> ParseAdditionalArgs(string? additionalArgs)
    {
        if (string.IsNullOrWhiteSpace(additionalArgs))
            return Array.Empty<string>();

        var matches = Regex.Matches(additionalArgs, "[\"].+?[\"]|[^ ]+");
        return matches
            .Select(match => match.Value.Trim().Trim('"'))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();
    }

    private static bool ContainsAuthTokenFlag(IEnumerable<string> args)
    {
        return args.Any(arg => string.Equals(arg, "--authtoken", StringComparison.OrdinalIgnoreCase));
    }
}
