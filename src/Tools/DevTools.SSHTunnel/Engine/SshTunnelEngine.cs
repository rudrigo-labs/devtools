using DevTools.Core.Abstractions;
using DevTools.Core.Models;
using DevTools.Core.Results;
using DevTools.Core.Validation;
using DevTools.SSHTunnel.Models;
using DevTools.SSHTunnel.Providers;
using DevTools.SSHTunnel.Validators;

namespace DevTools.SSHTunnel.Engine;

public sealed class SshTunnelEngine : IDevToolEngine<SshTunnelRequest, SshTunnelResult>
{
    private readonly TunnelService _service;
    private readonly IValidator<SshTunnelRequest> _validator;

    public SshTunnelEngine(IProcessRunner processRunner, IValidator<SshTunnelRequest>? validator = null)
    {
        var runner = processRunner as SSHTunnel.Providers.SshProcessRunner
            ?? new SshProcessRunner(processRunner);
        _service = new TunnelService(runner);
        _validator = validator ?? new SshTunnelRequestValidator();
    }

    // Construtor interno para injeção direta do TunnelService (testes)
    internal SshTunnelEngine(TunnelService service, IValidator<SshTunnelRequest>? validator = null)
    {
        _service = service;
        _validator = validator ?? new SshTunnelRequestValidator();
    }

    public async Task<RunResult<SshTunnelResult>> ExecuteAsync(
        SshTunnelRequest request,
        IProgressReporter? progress = null,
        CancellationToken cancellationToken = default)
    {
        var validation = _validator.Validate(request);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .Select(e => new ErrorDetail($"sshtunnel.{e.Field}", e.Message))
                .ToList();
            return RunResult<SshTunnelResult>.Fail(errors);
        }

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            switch (request.Action)
            {
                case SshTunnelAction.Start:
                    progress?.Report(new ProgressEvent("Iniciando SSH tunnel", 10, "start"));
                    await _service.StartAsync(request.Configuration!, null, cancellationToken).ConfigureAwait(false);
                    progress?.Report(new ProgressEvent("SSH tunnel ativo", 100, "done"));
                    return RunResult<SshTunnelResult>.Success(BuildResult(request.Action));

                case SshTunnelAction.Stop:
                    progress?.Report(new ProgressEvent("Encerrando SSH tunnel", 50, "stop"));
                    await _service.StopAsync(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);
                    progress?.Report(new ProgressEvent("SSH tunnel encerrado", 100, "done"));
                    return RunResult<SshTunnelResult>.Success(BuildResult(request.Action));

                case SshTunnelAction.Status:
                    return RunResult<SshTunnelResult>.Success(BuildResult(request.Action));

                default:
                    return RunResult<SshTunnelResult>.Fail(
                        new ErrorDetail("sshtunnel.action.invalid", "Action inválida."));
            }
        }
        catch (SshTunnelConfigException ex)
        {
            return RunResult<SshTunnelResult>.Fail(new ErrorDetail(ex.Code, ex.Message, Details: ex.Details, Exception: ex));
        }
        catch (SshTunnelConnectionException ex)
        {
            return RunResult<SshTunnelResult>.Fail(new ErrorDetail(ex.Code, ex.Message, Details: ex.Details, Exception: ex));
        }
    }

    public TunnelState CurrentState => _service.State;
    public bool IsRunning => _service.IsOn;
    public TunnelConfiguration? CurrentConfiguration => _service.CurrentConfiguration;

    private SshTunnelResult BuildResult(SshTunnelAction action) => new(
        action,
        _service.State,
        _service.IsOn,
        _service.CurrentConfiguration,
        _service.ProcessId,
        _service.LastError);
}
