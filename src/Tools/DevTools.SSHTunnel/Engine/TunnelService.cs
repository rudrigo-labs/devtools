using DevTools.Core.Abstractions;
using DevTools.SSHTunnel.Models;

namespace DevTools.SSHTunnel.Engine;

public sealed class TunnelService : IDisposable
{
    private readonly SshTunnelProcess _ssh;
    public TunnelConfiguration? CurrentConfiguration { get; private set; }
    public TunnelState State { get; private set; } = TunnelState.Off;
    public string? LastError { get; private set; }

    public bool IsOn => State == TunnelState.On && _ssh.IsRunning;

    public int? ProcessId => _ssh.ProcessId;

    public TunnelService(IProcessRunner runner)
    {
        _ssh = new SshTunnelProcess(runner ?? throw new ArgumentNullException(nameof(runner)));
    }

    public async Task StartAsync(TunnelConfiguration configuration, TimeSpan? startupTimeout = null, CancellationToken ct = default)
    {
        if (IsOn && CurrentConfiguration is not null && IsSameConfiguration(CurrentConfiguration, configuration))
            return;

        await StopAsync(TimeSpan.FromSeconds(2), ct).ConfigureAwait(false);

        if (!PortChecker.TryResolveBindHost(configuration.LocalBindHost, out _))
        {
            State = TunnelState.Error;
            LastError = $"Host de bind invÃ¡lido: {configuration.LocalBindHost}";
            throw new SshTunnelConfigException("sshtunnel.config.bind_host", LastError);
        }

        if (!PortChecker.IsPortFree(configuration.LocalBindHost, configuration.LocalPort))
        {
            State = TunnelState.Error;
            LastError = $"A porta {configuration.LocalBindHost}:{configuration.LocalPort} jÃ¡ estÃ¡ em uso.";
            throw new SshTunnelConfigException("sshtunnel.config.port_in_use", LastError);
        }

        var args = SshCommandBuilder.BuildArgs(configuration);
        var timeout = startupTimeout ?? TimeSpan.FromSeconds(configuration.ConnectTimeoutSeconds ?? 10);
        if (timeout <= TimeSpan.Zero)
            timeout = TimeSpan.FromSeconds(10);

        try
        {
            await _ssh.StartAsync(args, timeout, ct).ConfigureAwait(false);
            CurrentConfiguration = configuration;
            State = TunnelState.On;
            LastError = null;
        }
        catch (SshTunnelException)
        {
            State = TunnelState.Error;
            CurrentConfiguration = null;
            LastError = _ssh.LastResult?.StdErr ?? _ssh.LastResult?.StdOut;
            throw;
        }
        catch (Exception ex)
        {
            State = TunnelState.Error;
            CurrentConfiguration = null;
            LastError = ex.Message;
            throw new SshTunnelConnectionException("sshtunnel.connection.failed", "Falha ao iniciar o SSH.", ex.Message, ex);
        }
    }

    public async Task StopAsync(TimeSpan timeout, CancellationToken ct = default)
    {
        await _ssh.StopAsync(timeout, ct).ConfigureAwait(false);
        CurrentConfiguration = null;
        State = TunnelState.Off;
        LastError = null;
    }

    public void Dispose()
    {
        try
        {
            StopAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();
        }
        catch
        {
            // ignore
        }
    }

    private static bool IsSameConfiguration(TunnelConfiguration left, TunnelConfiguration right)
    {
        return string.Equals(left.Name, right.Name, StringComparison.OrdinalIgnoreCase)
            && string.Equals(left.SshHost, right.SshHost, StringComparison.OrdinalIgnoreCase)
            && left.SshPort == right.SshPort
            && string.Equals(left.SshUser, right.SshUser, StringComparison.OrdinalIgnoreCase)
            && string.Equals(left.LocalBindHost, right.LocalBindHost, StringComparison.OrdinalIgnoreCase)
            && left.LocalPort == right.LocalPort
            && string.Equals(left.RemoteHost, right.RemoteHost, StringComparison.OrdinalIgnoreCase)
            && left.RemotePort == right.RemotePort
            && string.Equals(left.IdentityFile ?? string.Empty, right.IdentityFile ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            && left.StrictHostKeyChecking == right.StrictHostKeyChecking
            && left.ConnectTimeoutSeconds == right.ConnectTimeoutSeconds;
    }
}


