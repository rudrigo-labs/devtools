using DevTools.Cli.Ui;
using DevTools.Cli.Logging;
using DevTools.Cli.App;
using DevTools.Ngrok.Engine;
using DevTools.Ngrok.Models;

namespace DevTools.Cli.Commands;

public sealed class NgrokCliCommand : ICliCommand
{
    private readonly CliConsole _ui;
    private readonly CliInput _input;
    private readonly NgrokEngine _engine;

    public NgrokCliCommand(CliConsole ui, CliInput input)
    {
        _ui = ui;
        _input = input;
        _engine = new NgrokEngine();
    }

    public string Key => "ngrok";
    public string Name => "Ngrok";
    public string Description => "Gerencia tuneis do ngrok (listar, iniciar, fechar, status).";

    public async Task<int> ExecuteAsync(CliLaunchOptions options, CancellationToken ct)
    {
        // 1. Resolve Parameters
        var actionStr = options.GetOption("action");
        var baseUrl = options.GetOption("base-url") ?? options.GetOption("url");
        var timeoutStr = options.GetOption("timeout");
        var retryStr = options.GetOption("retry");
        var tunnelName = options.GetOption("tunnel-name") ?? options.GetOption("name");
        var protocol = options.GetOption("protocol") ?? options.GetOption("proto");
        var portStr = options.GetOption("port");
        var ngrokPath = options.GetOption("ngrok-path") ?? options.GetOption("path");
        var extraArgsStr = options.GetOption("extra-args") ?? options.GetOption("args");

        int? timeout = int.TryParse(timeoutStr, out var parsedTimeout) ? parsedTimeout : null;
        int? retry = int.TryParse(retryStr, out var parsedRetry) ? parsedRetry : null;
        int? port = int.TryParse(portStr, out var parsedPort) ? parsedPort : null;

        NgrokAction? action = null;
        if (actionStr != null)
        {
            if (Enum.TryParse<NgrokAction>(actionStr, true, out var parsedAction)) action = parsedAction;
            else if (actionStr.Equals("list", StringComparison.OrdinalIgnoreCase)) action = NgrokAction.ListTunnels;
            else if (actionStr.Equals("close", StringComparison.OrdinalIgnoreCase)) action = NgrokAction.CloseTunnel;
            else if (actionStr.Equals("start", StringComparison.OrdinalIgnoreCase)) action = NgrokAction.StartHttp;
            else if (actionStr.Equals("kill", StringComparison.OrdinalIgnoreCase)) action = NgrokAction.KillAll;
            else if (actionStr.Equals("status", StringComparison.OrdinalIgnoreCase)) action = NgrokAction.Status;
        }

        // Interactive Fallback
        if (!options.IsNonInteractive)
        {
            if (action == null)
            {
                _ui.Section("Acoes");
                _ui.WriteLine("1) Listar tuneis");
                _ui.WriteLine("2) Fechar tunel");
                _ui.WriteLine("3) Start HTTP");
                _ui.WriteLine("4) Kill all");
                _ui.WriteLine("5) Status");

                var choice = _input.ReadInt("Escolha", 1, 5);
                action = choice switch
                {
                    1 => NgrokAction.ListTunnels,
                    2 => NgrokAction.CloseTunnel,
                    3 => NgrokAction.StartHttp,
                    4 => NgrokAction.KillAll,
                    _ => NgrokAction.Status
                };
            }

            if (string.IsNullOrWhiteSpace(baseUrl))
                baseUrl = _input.ReadOptional("BaseUrl API (opcional)", "enter = http://127.0.0.1:4040/");
            
            if (timeout == null)
                timeout = _input.ReadOptionalInt("Timeout (segundos)", "enter = 5") ?? 5;
            
            if (retry == null)
                retry = _input.ReadOptionalInt("Retry count", "enter = 1") ?? 1;

            if (action == NgrokAction.CloseTunnel && string.IsNullOrWhiteSpace(tunnelName))
            {
                tunnelName = _input.ReadRequired("Nome do tunel");
            }
            else if (action == NgrokAction.StartHttp)
            {
                if (string.IsNullOrWhiteSpace(protocol))
                    protocol = _input.ReadOptional("Protocolo", "http/https (enter = http)");
                
                if (port == null)
                    port = _input.ReadOptionalInt("Porta", "enter = 80") ?? 80;
                
                if (string.IsNullOrWhiteSpace(ngrokPath))
                    ngrokPath = _input.ReadOptional("Caminho do ngrok (opcional)", "enter = default");
                
                if (string.IsNullOrWhiteSpace(extraArgsStr))
                {
                    var extraList = _input.ReadCsv("Args extras (opcional)", "ex: --region=sa");
                    if (extraList.Count > 0)
                        extraArgsStr = string.Join(",", extraList);
                }
            }
        }

        // Defaults
        timeout ??= 5;
        retry ??= 1;

        // Validation
        if (action == null)
        {
            _ui.WriteError("Action required (--action list|close|start|kill|status).");
            return 1;
        }
        if (action == NgrokAction.CloseTunnel && string.IsNullOrWhiteSpace(tunnelName))
        {
            _ui.WriteError("Tunnel name required for close action (--tunnel-name).");
            return 1;
        }
        if (action == NgrokAction.StartHttp && port == null)
        {
            port = 80; // default if somehow missed
        }

        NgrokStartOptions? startOptions = null;
        if (action == NgrokAction.StartHttp)
        {
            var argsList = !string.IsNullOrWhiteSpace(extraArgsStr) 
                ? extraArgsStr.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList() 
                : null;

            startOptions = new NgrokStartOptions(
                string.IsNullOrWhiteSpace(protocol) ? "http" : protocol,
                port!.Value,
                string.IsNullOrWhiteSpace(ngrokPath) ? null : ngrokPath,
                argsList);
        }

        var request = new NgrokRequest(
            action.Value,
            string.IsNullOrWhiteSpace(baseUrl) ? null : baseUrl,
            timeout.Value,
            retry.Value,
            tunnelName,
            startOptions);

        using var progress = new CliProgressReporter(_ui.Theme);
        var result = await _engine.ExecuteAsync(request, progress, ct).ConfigureAwait(false);
        progress.Finish();

        if (!result.IsSuccess || result.Value is null)
        {
            WriteErrors(result.Errors);
            return 1;
        }

        var response = result.Value;
        
        if (!options.IsNonInteractive || action == NgrokAction.ListTunnels || action == NgrokAction.Status)
        {
            _ui.Section("Resultado");
            if (action == NgrokAction.ListTunnels && response.Tunnels is not null)
            {
                foreach (var t in response.Tunnels)
                    _ui.WriteLine($"{t.Name} | {t.PublicUrl} | {t.Proto} -> {t.Addr}");
            }
            else if (action == NgrokAction.CloseTunnel)
            {
                _ui.WriteLine(response.Closed == true ? "Tunel fechado." : "Tunel nao encontrado.");
            }
            else if (action == NgrokAction.StartHttp)
            {
                _ui.WriteLine(response.ProcessId.HasValue
                    ? $"Ngrok iniciado. PID: {response.ProcessId}"
                    : "Ngrok iniciado.");
            }
            else if (action == NgrokAction.KillAll)
            {
                _ui.WriteLine(response.Killed.HasValue
                    ? $"Processos encerrados: {response.Killed}"
                    : "Nenhum processo encontrado.");
            }
            else if (action == NgrokAction.Status)
            {
                _ui.WriteLine(response.HasAny == true ? "Ngrok esta em execucao." : "Ngrok nao encontrado.");
            }
        }

        return 0;
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
