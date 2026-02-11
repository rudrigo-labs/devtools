using DevTools.Cli.Ui;
using DevTools.Cli.Logging;
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

    public async Task<int> ExecuteAsync(CancellationToken ct)
    {
        _ui.Section("Acoes");
        _ui.WriteLine("1) Listar tuneis");
        _ui.WriteLine("2) Fechar tunel");
        _ui.WriteLine("3) Start HTTP");
        _ui.WriteLine("4) Kill all");
        _ui.WriteLine("5) Status");

        var choice = _input.ReadInt("Escolha", 1, 5);
        var action = choice switch
        {
            1 => NgrokAction.ListTunnels,
            2 => NgrokAction.CloseTunnel,
            3 => NgrokAction.StartHttp,
            4 => NgrokAction.KillAll,
            _ => NgrokAction.Status
        };

        var baseUrl = _input.ReadOptional("BaseUrl API (opcional)", "enter = http://127.0.0.1:4040/");
        var timeout = _input.ReadOptionalInt("Timeout (segundos)", "enter = 5") ?? 5;
        var retry = _input.ReadOptionalInt("Retry count", "enter = 1") ?? 1;

        string? tunnelName = null;
        NgrokStartOptions? startOptions = null;

        if (action == NgrokAction.CloseTunnel)
        {
            tunnelName = _input.ReadRequired("Nome do tunel");
        }
        else if (action == NgrokAction.StartHttp)
        {
            var protocol = _input.ReadOptional("Protocolo", "http/https (enter = http)");
            var port = _input.ReadOptionalInt("Porta", "enter = 80") ?? 80;
            var exe = _input.ReadOptional("Caminho do ngrok (opcional)", "enter = default");
            var extraArgs = _input.ReadCsv("Args extras (opcional)", "ex: --region=sa");

            startOptions = new NgrokStartOptions(
                string.IsNullOrWhiteSpace(protocol) ? "http" : protocol,
                port,
                string.IsNullOrWhiteSpace(exe) ? null : exe,
                extraArgs.Count == 0 ? null : extraArgs);
        }

        var request = new NgrokRequest(
            action,
            string.IsNullOrWhiteSpace(baseUrl) ? null : baseUrl,
            timeout,
            retry,
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
