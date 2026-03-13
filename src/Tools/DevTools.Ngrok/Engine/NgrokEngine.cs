using DevTools.Core.Abstractions;
using DevTools.Core.Models;
using DevTools.Core.Results;
using DevTools.Core.Validation;
using DevTools.Ngrok.Models;
using DevTools.Ngrok.Validators;

namespace DevTools.Ngrok.Engine;

public sealed class NgrokEngine : IDevToolEngine<NgrokRequest, NgrokResult>
{
    private readonly HttpClient _httpClient;
    private readonly NgrokProcessService _processService;
    private readonly IValidator<NgrokRequest> _validator;

    public NgrokEngine(
        HttpClient? httpClient = null,
        NgrokProcessService? processService = null,
        IValidator<NgrokRequest>? validator = null)
    {
        _httpClient     = httpClient     ?? new HttpClient();
        _processService = processService ?? new NgrokProcessService();
        _validator      = validator      ?? new NgrokRequestValidator();
    }

    public async Task<RunResult<NgrokResult>> ExecuteAsync(
        NgrokRequest request,
        IProgressReporter? progress = null,
        CancellationToken cancellationToken = default)
    {
        var validation = _validator.Validate(request);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .Select(e => new ErrorDetail($"ngrok.{e.Field}", e.Message))
                .ToList();
            return RunResult<NgrokResult>.Fail(errors);
        }

        var baseUrl = NormalizeBaseUrl(request.BaseUrl);
        var timeout = TimeSpan.FromSeconds(request.TimeoutSeconds <= 0 ? 5 : request.TimeoutSeconds);
        var retry   = Math.Max(0, request.RetryCount);

        try
        {
            switch (request.Action)
            {
                case NgrokAction.ListTunnels:
                {
                    progress?.Report(new ProgressEvent("Buscando tunnels", 20, "api"));
                    var client  = new NgrokApiClient(_httpClient, new Uri(baseUrl));
                    var tunnels = await client.GetTunnelsAsync(timeout, retry, cancellationToken).ConfigureAwait(false);
                    var groups  = TunnelGrouping.GroupByBaseName(tunnels);
                    progress?.Report(new ProgressEvent("Tunnels obtidos", 100, "done"));
                    return RunResult<NgrokResult>.Success(new NgrokResult(request.Action, baseUrl, tunnels, groups));
                }

                case NgrokAction.CloseTunnel:
                {
                    progress?.Report(new ProgressEvent("Fechando tunnel", 40, "api"));
                    var client = new NgrokApiClient(_httpClient, new Uri(baseUrl));
                    var closed = await client.CloseTunnelAsync(request.TunnelName!, timeout, retry, cancellationToken).ConfigureAwait(false);
                    progress?.Report(new ProgressEvent("Tunnel fechado", 100, "done"));
                    return RunResult<NgrokResult>.Success(new NgrokResult(request.Action, baseUrl, Closed: closed));
                }

                case NgrokAction.StartHttp:
                {
                    progress?.Report(new ProgressEvent("Iniciando ngrok", 20, "start"));
                    var options    = request.StartOptions!;
                    var normalized = new NgrokStartOptions(NormalizeProtocol(options.Protocol), options.Port, options.ExecutablePath, options.ExtraArgs);
                    var pid        = _processService.StartHttp(normalized);
                    progress?.Report(new ProgressEvent("Ngrok iniciado", 100, "done"));
                    return RunResult<NgrokResult>.Success(new NgrokResult(request.Action, baseUrl, ProcessId: pid));
                }

                case NgrokAction.KillAll:
                {
                    var killed = _processService.KillAll();
                    return RunResult<NgrokResult>.Success(new NgrokResult(request.Action, baseUrl, Killed: killed));
                }

                case NgrokAction.Status:
                {
                    var hasAny = _processService.HasAny();
                    return RunResult<NgrokResult>.Success(new NgrokResult(request.Action, baseUrl, HasAny: hasAny));
                }

                default:
                    return RunResult<NgrokResult>.Fail(new ErrorDetail("ngrok.action.invalid", "Action inválida."));
            }
        }
        catch (NgrokApiException ex)
        {
            return RunResult<NgrokResult>.Fail(new ErrorDetail(ex.Code, ex.Message, Details: ex.Details, Exception: ex));
        }
    }

    private static string NormalizeBaseUrl(string? baseUrl)
    {
        var url = string.IsNullOrWhiteSpace(baseUrl) ? "http://127.0.0.1:4040/" : baseUrl.Trim();
        if (!url.EndsWith("/", StringComparison.Ordinal)) url += "/";
        return url;
    }

    private static string NormalizeProtocol(string? protocol) =>
        string.IsNullOrWhiteSpace(protocol) ? "http" :
        protocol.Equals("https", StringComparison.OrdinalIgnoreCase) ? "https" : "http";
}
