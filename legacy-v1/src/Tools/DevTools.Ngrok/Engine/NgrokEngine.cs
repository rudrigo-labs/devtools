using DevTools.Core.Abstractions;
using DevTools.Core.Models;
using DevTools.Core.Results;
using DevTools.Ngrok.Models;
using DevTools.Ngrok.Validation;

namespace DevTools.Ngrok.Engine;

public sealed class NgrokEngine : IDevToolEngine<NgrokRequest, NgrokResponse>
{
    private readonly HttpClient _httpClient;
    private readonly NgrokProcessService _processService;

    public NgrokEngine(HttpClient? httpClient = null, NgrokProcessService? processService = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _processService = processService ?? new NgrokProcessService();
    }

    public async Task<RunResult<NgrokResponse>> ExecuteAsync(
        NgrokRequest request,
        IProgressReporter? progress = null,
        CancellationToken ct = default)
    {
        var errors = NgrokRequestValidator.Validate(request);
        if (errors.Count > 0)
            return RunResult<NgrokResponse>.Fail(errors);

        var baseUrl = NormalizeBaseUrl(request.BaseUrl);
        var timeout = TimeSpan.FromSeconds(request.TimeoutSeconds <= 0 ? 5 : request.TimeoutSeconds);
        var retry = Math.Max(0, request.RetryCount);

        try
        {
            switch (request.Action)
            {
                case NgrokAction.ListTunnels:
                {
                    progress?.Report(new ProgressEvent("Fetching tunnels", 20, "api"));
                    var client = new NgrokApiClient(_httpClient, new Uri(baseUrl));
                    var tunnels = await client.GetTunnelsAsync(timeout, retry, ct).ConfigureAwait(false);
                    var groups = TunnelGrouping.GroupByBaseName(tunnels);
                    progress?.Report(new ProgressEvent("Tunnels fetched", 100, "done"));
                    return RunResult<NgrokResponse>.Success(new NgrokResponse(request.Action, baseUrl, tunnels, groups));
                }
                case NgrokAction.CloseTunnel:
                {
                    progress?.Report(new ProgressEvent("Closing tunnel", 40, "api"));
                    var client = new NgrokApiClient(_httpClient, new Uri(baseUrl));
                    var closed = await client.CloseTunnelAsync(request.TunnelName!, timeout, retry, ct).ConfigureAwait(false);
                    progress?.Report(new ProgressEvent("Tunnel closed", 100, "done"));
                    return RunResult<NgrokResponse>.Success(new NgrokResponse(request.Action, baseUrl, Closed: closed));
                }
                case NgrokAction.StartHttp:
                {
                    progress?.Report(new ProgressEvent("Starting ngrok", 20, "start"));
                    var options = request.StartOptions!;
                    var normalized = new NgrokStartOptions(
                        NormalizeProtocol(options.Protocol),
                        options.Port,
                        options.ExecutablePath,
                        options.ExtraArgs);

                    var pid = _processService.StartHttp(normalized);
                    progress?.Report(new ProgressEvent("Ngrok started", 100, "done"));
                    return RunResult<NgrokResponse>.Success(new NgrokResponse(request.Action, baseUrl, ProcessId: pid));
                }
                case NgrokAction.KillAll:
                {
                    var killed = _processService.KillAll();
                    return RunResult<NgrokResponse>.Success(new NgrokResponse(request.Action, baseUrl, Killed: killed));
                }
                case NgrokAction.Status:
                {
                    var hasAny = _processService.HasAny();
                    return RunResult<NgrokResponse>.Success(new NgrokResponse(request.Action, baseUrl, HasAny: hasAny));
                }
                default:
                    return RunResult<NgrokResponse>.Fail(new ErrorDetail("ngrok.action.invalid", "Action is invalid."));
            }
        }
        catch (NgrokApiException ex)
        {
            return RunResult<NgrokResponse>.Fail(new ErrorDetail(ex.Code, ex.Message, Details: ex.Details, Exception: ex));
        }
    }

    private static string NormalizeBaseUrl(string? baseUrl)
    {
        var url = string.IsNullOrWhiteSpace(baseUrl)
            ? "http://127.0.0.1:4040/"
            : baseUrl.Trim();

        if (!url.EndsWith("/", StringComparison.Ordinal))
            url += "/";

        return url;
    }

    private static string NormalizeProtocol(string? protocol)
    {
        if (string.IsNullOrWhiteSpace(protocol))
            return "http";

        return protocol.Equals("https", StringComparison.OrdinalIgnoreCase) ? "https" : "http";
    }
}
