using DevTools.Core.Abstractions;
using DevTools.Core.Models;
using DevTools.Core.Results;
using DevTools.Core.Validation;
using DevTools.SSHTunnel.Models;
using DevTools.SSHTunnel.Providers;
using DevTools.SSHTunnel.Validators;

namespace DevTools.SSHTunnel.Engine;

public sealed class SshTunnelEngine : IDevToolEngine<SshTunnelRequest, SshTunnelResult>, IDisposable
{
    private const string SingleServiceKey = "__single__";

    private readonly object _sync = new();
    private readonly Dictionary<string, TunnelService> _services = new(StringComparer.OrdinalIgnoreCase);
    private readonly IProcessRunner? _processRunner;
    private readonly IValidator<SshTunnelRequest> _validator;
    private readonly bool _singleServiceMode;

    public SshTunnelEngine(IProcessRunner processRunner, IValidator<SshTunnelRequest>? validator = null)
    {
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
        _validator = validator ?? new SshTunnelRequestValidator();
    }

    // Construtor interno para injeção direta do TunnelService (testes).
    internal SshTunnelEngine(TunnelService service, IValidator<SshTunnelRequest>? validator = null)
    {
        _validator = validator ?? new SshTunnelRequestValidator();
        _singleServiceMode = true;
        _services[SingleServiceKey] = service ?? throw new ArgumentNullException(nameof(service));
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
                {
                    progress?.Report(new ProgressEvent("Iniciando SSH tunnel", 10, "start"));
                    var configuration = request.Configuration!;
                    var key = ResolveTunnelKey(configuration);
                    var service = GetOrCreateService(key);
                    await service.StartAsync(configuration, null, cancellationToken).ConfigureAwait(false);
                    progress?.Report(new ProgressEvent("SSH tunnel ativo", 100, "done"));
                    return RunResult<SshTunnelResult>.Success(BuildResult(request.Action, key));
                }

                case SshTunnelAction.Stop:
                {
                    progress?.Report(new ProgressEvent("Encerrando SSH tunnel", 50, "stop"));

                    var hasSpecificTarget = request.Configuration is not null
                        && !string.IsNullOrWhiteSpace(request.Configuration.Name);

                    if (hasSpecificTarget)
                    {
                        var key = ResolveTunnelKey(request.Configuration!);
                        await StopByKeyAsync(key, cancellationToken).ConfigureAwait(false);
                        progress?.Report(new ProgressEvent("SSH tunnel encerrado", 100, "done"));
                        return RunResult<SshTunnelResult>.Success(BuildResult(request.Action, key));
                    }

                    await StopAllAsync(cancellationToken).ConfigureAwait(false);
                    progress?.Report(new ProgressEvent("SSH tunnel encerrado", 100, "done"));
                    return RunResult<SshTunnelResult>.Success(BuildResult(request.Action, null));
                }

                case SshTunnelAction.Status:
                    return RunResult<SshTunnelResult>.Success(BuildResult(request.Action, null));

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

    public TunnelState CurrentState
    {
        get
        {
            lock (_sync)
            {
                var states = _services.Values.Select(x => x.State).ToList();
                if (states.Any(s => s == TunnelState.Error)) return TunnelState.Error;
                if (states.Any(s => s == TunnelState.On)) return TunnelState.On;
                return TunnelState.Off;
            }
        }
    }

    public bool IsRunning
    {
        get
        {
            lock (_sync)
                return _services.Values.Any(x => x.IsOn);
        }
    }

    public TunnelConfiguration? CurrentConfiguration => ActiveTunnels.FirstOrDefault()?.Configuration;
    public int? CurrentProcessId => ActiveTunnels.FirstOrDefault()?.ProcessId;

    public IReadOnlyList<SshActiveTunnelInfo> ActiveTunnels
    {
        get
        {
            lock (_sync)
            {
                return _services
                    .Select(x => CreateActiveInfo(x.Key, x.Value))
                    .Where(x => x is not null)
                    .Select(x => x!)
                    .ToList();
            }
        }
    }

    public void Dispose()
    {
        List<TunnelService> servicesToDispose;
        lock (_sync)
        {
            servicesToDispose = _services.Values.ToList();
            _services.Clear();
        }

        foreach (var service in servicesToDispose)
            service.Dispose();
    }

    private SshTunnelResult BuildResult(SshTunnelAction action, string? key)
    {
        var selected = GetSelectedServiceInfo(key);
        return new SshTunnelResult(
            action,
            CurrentState,
            IsRunning,
            selected?.Configuration ?? CurrentConfiguration,
            selected?.ProcessId ?? CurrentProcessId,
            selected?.LastError);
    }

    private SshActiveTunnelInfo? GetSelectedServiceInfo(string? key)
    {
        lock (_sync)
        {
            if (!string.IsNullOrWhiteSpace(key) && _services.TryGetValue(key, out var specific))
                return CreateActiveInfo(key, specific);

            foreach (var pair in _services)
            {
                var info = CreateActiveInfo(pair.Key, pair.Value);
                if (info is not null)
                    return info;
            }

            return null;
        }
    }

    private static SshActiveTunnelInfo? CreateActiveInfo(string key, TunnelService service)
    {
        if (!service.IsOn || service.CurrentConfiguration is null)
            return null;

        return new SshActiveTunnelInfo(
            key,
            service.State,
            service.CurrentConfiguration,
            service.ProcessId,
            service.LastError);
    }

    private async Task StopByKeyAsync(string key, CancellationToken ct)
    {
        TunnelService? service;
        lock (_sync)
            _services.TryGetValue(key, out service);

        if (service is null)
            return;

        await service.StopAsync(TimeSpan.FromSeconds(2), ct).ConfigureAwait(false);

        if (_singleServiceMode)
            return;

        lock (_sync)
        {
            if (_services.TryGetValue(key, out var current) && ReferenceEquals(current, service))
            {
                _services.Remove(key);
                current.Dispose();
            }
        }
    }

    private async Task StopAllAsync(CancellationToken ct)
    {
        List<KeyValuePair<string, TunnelService>> snapshot;
        lock (_sync)
            snapshot = _services.ToList();

        foreach (var pair in snapshot)
            await pair.Value.StopAsync(TimeSpan.FromSeconds(2), ct).ConfigureAwait(false);

        if (_singleServiceMode)
            return;

        lock (_sync)
        {
            foreach (var pair in snapshot)
                pair.Value.Dispose();
            _services.Clear();
        }
    }

    private TunnelService GetOrCreateService(string key)
    {
        lock (_sync)
        {
            if (_services.TryGetValue(key, out var existing))
                return existing;

            if (_singleServiceMode && _services.TryGetValue(SingleServiceKey, out var singleService))
                return singleService;

            var service = CreateService();
            _services[key] = service;
            return service;
        }
    }

    private TunnelService CreateService()
    {
        if (_processRunner is null)
            throw new InvalidOperationException("ProcessRunner não disponível.");

        // Cada túnel mantém sua própria instância para capturar PID sem interferência.
        return new TunnelService(new SshProcessRunner(_processRunner));
    }

    private static string ResolveTunnelKey(TunnelConfiguration configuration)
    {
        var name = configuration.Name?.Trim();
        if (!string.IsNullOrWhiteSpace(name))
            return name;

        return $"{configuration.SshUser}@{configuration.SshHost}:{configuration.SshPort}|{configuration.LocalBindHost}:{configuration.LocalPort}->{configuration.RemoteHost}:{configuration.RemotePort}";
    }
}
