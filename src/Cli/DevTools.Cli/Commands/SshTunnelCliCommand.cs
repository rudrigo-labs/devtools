using DevTools.Cli.Ui;
using DevTools.Cli.Logging;
using DevTools.Cli.App;
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

    public async Task<int> ExecuteAsync(CliLaunchOptions options, CancellationToken ct)
    {
        // 1. Resolve Action
        var actionStr = options.GetOption("action");
        SshTunnelAction? action = null;
        if (actionStr != null)
        {
            if (Enum.TryParse<SshTunnelAction>(actionStr, true, out var a)) action = a;
            else if (actionStr.Equals("start", StringComparison.OrdinalIgnoreCase)) action = SshTunnelAction.Start;
            else if (actionStr.Equals("stop", StringComparison.OrdinalIgnoreCase)) action = SshTunnelAction.Stop;
            else if (actionStr.Equals("status", StringComparison.OrdinalIgnoreCase)) action = SshTunnelAction.Status;
        }

        if (action == null && !options.IsNonInteractive)
        {
            _ui.Section("Acoes");
            _ui.WriteLine("1) Iniciar tunel");
            _ui.WriteLine("2) Parar tunel");
            _ui.WriteLine("3) Status");

            var choice = _input.ReadInt("Escolha", 1, 3);
            action = choice switch
            {
                1 => SshTunnelAction.Start,
                2 => SshTunnelAction.Stop,
                _ => SshTunnelAction.Status
            };
        }

        if (action == null)
        {
            _ui.WriteError("Action required (--action start|stop|status).");
            return 1;
        }

        // 2. Resolve Parameters
        TunnelProfile? profile = null;
        if (action == SshTunnelAction.Start)
        {
            profile = ResolveProfile(options);
        }

        var request = new SshTunnelRequest(action.Value, profile);

        using var progress = new CliProgressReporter(_ui.Theme);
        var result = await _engine.ExecuteAsync(request, progress, ct).ConfigureAwait(false);
        progress.Finish();

        if (!result.IsSuccess || result.Value is null)
        {
            WriteErrors(result.Errors);
            return 1;
        }

        var response = result.Value;
        
        if (!options.IsNonInteractive || action == SshTunnelAction.Status)
        {
            _ui.Section("Status");
            _ui.WriteKeyValue("Estado", response.State.ToString());
            _ui.WriteKeyValue("Rodando", response.IsRunning ? "Sim" : "Nao");
            if (response.Profile is not null)
                _ui.WriteKeyValue("Perfil", response.Profile.Name);
            if (response.ProcessId.HasValue)
                _ui.WriteKeyValue("PID", response.ProcessId.Value.ToString());
            if (!string.IsNullOrWhiteSpace(response.LastError))
                _ui.WriteWarning(response.LastError);
        }

        return response.IsRunning || action != SshTunnelAction.Start ? 0 : 1;
    }

    private TunnelProfile ResolveProfile(CliLaunchOptions options)
    {
        // Args
        var name = options.GetOption("profile") ?? options.GetOption("name");
        var sshHost = options.GetOption("ssh-host") ?? options.GetOption("host");
        var sshPortStr = options.GetOption("ssh-port") ?? options.GetOption("port");
        var sshUser = options.GetOption("ssh-user") ?? options.GetOption("user");
        var localBind = options.GetOption("local-bind") ?? options.GetOption("bind");
        var localPortStr = options.GetOption("local-port");
        var remoteHost = options.GetOption("remote-host");
        var remotePortStr = options.GetOption("remote-port");
        var identity = options.GetOption("identity") ?? options.GetOption("key");
        var strictStr = options.GetOption("strict-host-key") ?? options.GetOption("strict");
        var timeoutStr = options.GetOption("timeout");

        int? sshPort = int.TryParse(sshPortStr, out var p1) ? p1 : null;
        int? localPort = int.TryParse(localPortStr, out var p2) ? p2 : null;
        int? remotePort = int.TryParse(remotePortStr, out var p3) ? p3 : null;
        int? timeout = int.TryParse(timeoutStr, out var p4) ? p4 : null;

        SshStrictHostKeyChecking strict = SshStrictHostKeyChecking.Default;
        if (strictStr != null)
        {
             if (Enum.TryParse<SshStrictHostKeyChecking>(strictStr, true, out var s)) strict = s;
             else if (strictStr.Equals("yes", StringComparison.OrdinalIgnoreCase)) strict = SshStrictHostKeyChecking.Yes;
             else if (strictStr.Equals("no", StringComparison.OrdinalIgnoreCase)) strict = SshStrictHostKeyChecking.No;
             else if (strictStr.Equals("accept-new", StringComparison.OrdinalIgnoreCase)) strict = SshStrictHostKeyChecking.AcceptNew;
        }

        // Interactive Fallback
        if (!options.IsNonInteractive)
        {
            if (string.IsNullOrWhiteSpace(name))
                name = _input.ReadOptional("Nome do perfil", "enter = default");
            
            if (string.IsNullOrWhiteSpace(sshHost))
                sshHost = _input.ReadRequired("Host SSH", "ex: 10.0.0.10");
            
            if (sshPort == null)
                sshPort = _input.ReadOptionalInt("Porta SSH", "enter = 22") ?? 22;
            
            if (string.IsNullOrWhiteSpace(sshUser))
                sshUser = _input.ReadRequired("Usuario SSH", "ex: dev");

            if (string.IsNullOrWhiteSpace(localBind))
                localBind = _input.ReadOptional("Bind local", "enter = 127.0.0.1");
            
            if (localPort == null)
                localPort = _input.ReadOptionalInt("Porta local", "enter = 14331") ?? 14331;
            
            if (string.IsNullOrWhiteSpace(remoteHost))
                remoteHost = _input.ReadOptional("Host remoto", "enter = 127.0.0.1");
            
            if (remotePort == null)
                remotePort = _input.ReadOptionalInt("Porta remota", "enter = 1433") ?? 1433;

            if (string.IsNullOrWhiteSpace(identity))
                identity = _input.ReadOptional("Identity file (opcional)", "enter = padrao");

            if (strictStr == null)
            {
                _ui.Section("StrictHostKeyChecking");
                _ui.WriteLine("1) Default");
                _ui.WriteLine("2) Yes");
                _ui.WriteLine("3) No");
                _ui.WriteLine("4) AcceptNew");
                var strictChoice = _input.ReadInt("Escolha", 1, 4);
                strict = strictChoice switch
                {
                    2 => SshStrictHostKeyChecking.Yes,
                    3 => SshStrictHostKeyChecking.No,
                    4 => SshStrictHostKeyChecking.AcceptNew,
                    _ => SshStrictHostKeyChecking.Default
                };
            }

            if (timeout == null)
                timeout = _input.ReadOptionalInt("Timeout conexao (segundos)", "enter = 10");
        }

        // Defaults for required fields if missing in non-interactive (though strict check below will catch them)
        sshPort ??= 22;
        localPort ??= 14331;
        remotePort ??= 1433;
        
        // Validation for required fields
        if (string.IsNullOrWhiteSpace(sshHost)) throw new ArgumentException("SSH Host required (--ssh-host)");
        if (string.IsNullOrWhiteSpace(sshUser)) throw new ArgumentException("SSH User required (--ssh-user)");

        return new TunnelProfile
        {
            Name = string.IsNullOrWhiteSpace(name) ? "default" : name,
            SshHost = sshHost,
            SshPort = sshPort.Value,
            SshUser = sshUser,
            LocalBindHost = string.IsNullOrWhiteSpace(localBind) ? "127.0.0.1" : localBind,
            LocalPort = localPort.Value,
            RemoteHost = string.IsNullOrWhiteSpace(remoteHost) ? "127.0.0.1" : remoteHost,
            RemotePort = remotePort.Value,
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
