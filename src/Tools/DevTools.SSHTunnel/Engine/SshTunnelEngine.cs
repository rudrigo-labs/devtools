using DevTools.Core.Abstractions;
using DevTools.Core.Models;
using DevTools.Core.Results;
using DevTools.SSHTunnel.Models;
using DevTools.SSHTunnel.Providers;
using DevTools.SSHTunnel.Validation;

namespace DevTools.SSHTunnel.Engine;

public sealed class SshTunnelEngine : IDevToolEngine<SshTunnelRequest, SshTunnelResponse>
{
    private readonly TunnelService _service;

    public SshTunnelEngine(TunnelService? service = null, IProcessRunner? processRunner = null)
    {
        if (service is not null)
        {
            _service = service;
        }
        else
        {
            var runner = processRunner ?? new SystemProcessRunner();
            _service = new TunnelService(runner);
        }
    }

    public async Task<RunResult<SshTunnelResponse>> ExecuteAsync(
        SshTunnelRequest request,
        IProgressReporter? progress = null,
        CancellationToken ct = default)
    {
        var errors = SshTunnelRequestValidator.Validate(request);
        if (errors.Count > 0)
            return RunResult<SshTunnelResponse>.Fail(errors);

        ct.ThrowIfCancellationRequested();

        try
        {
            switch (request.Action)
            {
                case SshTunnelAction.Start:
                    progress?.Report(new ProgressEvent("Starting SSH tunnel", 10, "start"));
                    await _service.StartAsync(request.Profile!, null, ct).ConfigureAwait(false);
                    progress?.Report(new ProgressEvent("SSH tunnel running", 100, "done"));
                    return RunResult<SshTunnelResponse>.Success(BuildResponse(request.Action));

                case SshTunnelAction.Stop:
                    progress?.Report(new ProgressEvent("Stopping SSH tunnel", 50, "stop"));
                    await _service.StopAsync(TimeSpan.FromSeconds(2), ct).ConfigureAwait(false);
                    progress?.Report(new ProgressEvent("SSH tunnel stopped", 100, "done"));
                    return RunResult<SshTunnelResponse>.Success(BuildResponse(request.Action));

                case SshTunnelAction.Status:
                    return RunResult<SshTunnelResponse>.Success(BuildResponse(request.Action));

                default:
                    return RunResult<SshTunnelResponse>.Fail(
                        new ErrorDetail("sshtunnel.action.invalid", "Action is invalid."));
            }
        }
        catch (SshTunnelConfigException ex)
        {
            return RunResult<SshTunnelResponse>.Fail(new ErrorDetail(ex.Code, ex.Message, Details: ex.Details, Exception: ex));
        }
        catch (SshTunnelConnectionException ex)
        {
            return RunResult<SshTunnelResponse>.Fail(new ErrorDetail(ex.Code, ex.Message, Details: ex.Details, Exception: ex));
        }
    }

    private SshTunnelResponse BuildResponse(SshTunnelAction action)
    {
        return new SshTunnelResponse(
            action,
            _service.State,
            _service.IsOn,
            _service.CurrentProfile,
            _service.ProcessId,
            _service.LastError);
    }
}
