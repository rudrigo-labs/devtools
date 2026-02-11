using DevTools.Core.Abstractions;
using DevTools.SSHTunnel.Models;

namespace DevTools.SSHTunnel.Engine;

public sealed class TunnelService : IDisposable
{
    private readonly SshTunnelProcess _ssh;
    public TunnelProfile? CurrentProfile { get; private set; }
    public TunnelState State { get; private set; } = TunnelState.Off;
    public string? LastError { get; private set; }

    public bool IsOn => State == TunnelState.On && _ssh.IsRunning;

    public int? ProcessId => _ssh.ProcessId;

    public TunnelService(IProcessRunner runner)
    {
        _ssh = new SshTunnelProcess(runner ?? throw new ArgumentNullException(nameof(runner)));
    }

    public async Task StartAsync(TunnelProfile profile, TimeSpan? startupTimeout = null, CancellationToken ct = default)
    {
        if (IsOn && CurrentProfile?.Name == profile.Name)
            return;

        await StopAsync(TimeSpan.FromSeconds(2), ct).ConfigureAwait(false);

        if (!PortChecker.TryResolveBindHost(profile.LocalBindHost, out _))
        {
            State = TunnelState.Error;
            LastError = $"Host de bind inválido: {profile.LocalBindHost}";
            throw new SshTunnelConfigException("sshtunnel.config.bind_host", LastError);
        }

        if (!PortChecker.IsPortFree(profile.LocalBindHost, profile.LocalPort))
        {
            State = TunnelState.Error;
            LastError = $"A porta {profile.LocalBindHost}:{profile.LocalPort} já está em uso.";
            throw new SshTunnelConfigException("sshtunnel.config.port_in_use", LastError);
        }

        var args = SshCommandBuilder.BuildArgs(profile);
        var timeout = startupTimeout ?? TimeSpan.FromSeconds(profile.ConnectTimeoutSeconds ?? 10);
        if (timeout <= TimeSpan.Zero)
            timeout = TimeSpan.FromSeconds(10);

        try
        {
            await _ssh.StartAsync(args, timeout, ct).ConfigureAwait(false);
            CurrentProfile = profile;
            State = TunnelState.On;
            LastError = null;
        }
        catch (SshTunnelException)
        {
            State = TunnelState.Error;
            CurrentProfile = null;
            LastError = _ssh.LastResult?.StdErr ?? _ssh.LastResult?.StdOut;
            throw;
        }
        catch (Exception ex)
        {
            State = TunnelState.Error;
            CurrentProfile = null;
            LastError = ex.Message;
            throw new SshTunnelConnectionException("sshtunnel.connection.failed", "Falha ao iniciar o SSH.", ex.Message, ex);
        }
    }

    public async Task StopAsync(TimeSpan timeout, CancellationToken ct = default)
    {
        await _ssh.StopAsync(timeout, ct).ConfigureAwait(false);
        CurrentProfile = null;
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
}
