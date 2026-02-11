using DevTools.Cli.Ui;
using DevTools.Cli.Logging;
using DevTools.SSHTunnel.Engine;
using DevTools.SSHTunnel.Models;
using DevTools.SSHTunnel.Providers;

namespace DevTools.Cli.Commands;

public sealed class SshTunnelCliCommand : ICliCommand
{
    private static readonly TunnelService SharedService = new(new SystemProcessRunner());
    private readonly SshTunnelEngine _engine;
    private readonly CliConsole _ui;
    private readonly CliInput _input;

    public SshTunnelCliCommand(CliConsole ui, CliInput input)
    {
        _ui = ui;
        _input = input;
        _engine = new SshTunnelEngine(SharedService);
    }

    public string Key => "sshtunnel";
    public string Name => "SSH Tunnel";
    public string Description => "Cria e encerra tuneis SSH locais com status e perfis.";

    public async Task<int> ExecuteAsync(CancellationToken ct)
    {
        _ui.Section("Acoes");
        _ui.WriteLine("1) Iniciar tunel");
        _ui.WriteLine("2) Parar tunel");
        _ui.WriteLine("3) Status");

        var choice = _input.ReadInt("Escolha", 1, 3);
        var action = choice switch
        {
            1 => SshTunnelAction.Start,
            2 => SshTunnelAction.Stop,
            _ => SshTunnelAction.Status
        };

        TunnelProfile? profile = null;
        if (action == SshTunnelAction.Start)
            profile = BuildProfile();

        var request = new SshTunnelRequest(action, profile);

        using var progress = new CliProgressReporter(_ui.Theme);
        var result = await _engine.ExecuteAsync(request, progress, ct).ConfigureAwait(false);
        progress.Finish();

        if (!result.IsSuccess || result.Value is null)
        {
            WriteErrors(result.Errors);
            return 1;
        }

        var response = result.Value;
        _ui.Section("Status");
        _ui.WriteKeyValue("Estado", response.State.ToString());
        _ui.WriteKeyValue("Rodando", response.IsRunning ? "Sim" : "Nao");
        if (response.Profile is not null)
            _ui.WriteKeyValue("Perfil", response.Profile.Name);
        if (response.ProcessId.HasValue)
            _ui.WriteKeyValue("PID", response.ProcessId.Value.ToString());
        if (!string.IsNullOrWhiteSpace(response.LastError))
            _ui.WriteWarning(response.LastError);

        return response.IsRunning || action != SshTunnelAction.Start ? 0 : 1;
    }

    private TunnelProfile BuildProfile()
    {
        var name = _input.ReadOptional("Nome do perfil", "enter = default");
        var sshHost = _input.ReadRequired("Host SSH", "ex: 10.0.0.10");
        var sshPort = _input.ReadOptionalInt("Porta SSH", "enter = 22") ?? 22;
        var sshUser = _input.ReadRequired("Usuario SSH", "ex: dev");

        var localBind = _input.ReadOptional("Bind local", "enter = 127.0.0.1");
        var localPort = _input.ReadOptionalInt("Porta local", "enter = 14331") ?? 14331;
        var remoteHost = _input.ReadOptional("Host remoto", "enter = 127.0.0.1");
        var remotePort = _input.ReadOptionalInt("Porta remota", "enter = 1433") ?? 1433;

        var identity = _input.ReadOptional("Identity file (opcional)", "enter = padrao");

        _ui.Section("StrictHostKeyChecking");
        _ui.WriteLine("1) Default");
        _ui.WriteLine("2) Yes");
        _ui.WriteLine("3) No");
        _ui.WriteLine("4) AcceptNew");
        var strictChoice = _input.ReadInt("Escolha", 1, 4);
        var strict = strictChoice switch
        {
            2 => SshStrictHostKeyChecking.Yes,
            3 => SshStrictHostKeyChecking.No,
            4 => SshStrictHostKeyChecking.AcceptNew,
            _ => SshStrictHostKeyChecking.Default
        };

        var timeout = _input.ReadOptionalInt("Timeout conexao (segundos)", "enter = 10");

        return new TunnelProfile
        {
            Name = string.IsNullOrWhiteSpace(name) ? "default" : name,
            SshHost = sshHost,
            SshPort = sshPort,
            SshUser = sshUser,
            LocalBindHost = string.IsNullOrWhiteSpace(localBind) ? "127.0.0.1" : localBind,
            LocalPort = localPort,
            RemoteHost = string.IsNullOrWhiteSpace(remoteHost) ? "127.0.0.1" : remoteHost,
            RemotePort = remotePort,
            IdentityFile = string.IsNullOrWhiteSpace(identity) ? null : identity,
            StrictHostKeyChecking = strict,
            ConnectTimeoutSeconds = timeout
        };
    }

    private void WriteErrors(IReadOnlyList<DevTools.Core.Results.ErrorDetail> errors)
    {
        CliErrorLogger.LogErrors(Key, errors);
        _ui.Section("Erros");
        foreach (var error in errors)
        {
            _ui.WriteError($"{error.Code}: {error.Message}");
            if (!string.IsNullOrWhiteSpace(error.Details))
                _ui.WriteDim(error.Details);
        }
    }
}
