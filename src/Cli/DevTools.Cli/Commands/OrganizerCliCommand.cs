using DevTools.Cli.Ui;
using DevTools.Cli.Logging;
using DevTools.Organizer.Engine;
using DevTools.Organizer.Models;

namespace DevTools.Cli.Commands;

public sealed class OrganizerCliCommand : ICliCommand
{
    private readonly CliConsole _ui;
    private readonly CliInput _input;
    private readonly OrganizerEngine _engine;

    public OrganizerCliCommand(CliConsole ui, CliInput input)
    {
        _ui = ui;
        _input = input;
        _engine = new OrganizerEngine();
    }

    public string Key => "organizer";
    public string Name => "Organizer";
    public string Description => "Classifica documentos por assunto, deduplica e move.";

    public async Task<int> ExecuteAsync(CancellationToken ct)
    {
        var inbox = _input.ReadRequired("Pasta de entrada", "ex: C:\\Projetos\\Inbox");
        var output = _input.ReadRequired("Pasta de saida", "ex: C:\\Projetos\\Organizado");
        var config = _input.ReadOptional("Config (opcional)", "enter para padrao");
        var minScore = _input.ReadOptionalInt("MinScore (opcional)");
        var apply = _input.ReadYesNo("Aplicar mudancas", false);

        var request = new OrganizerRequest(
            inbox,
            output,
            string.IsNullOrWhiteSpace(config) ? null : config,
            minScore,
            apply);

        using var progress = new CliProgressReporter(_ui.Theme);
        var result = await _engine.ExecuteAsync(request, progress, ct).ConfigureAwait(false);
        progress.Finish();

        if (!result.IsSuccess || result.Value is null)
        {
            WriteErrors(result.Errors);
            return 1;
        }

        var response = result.Value;
        _ui.Section("Resumo");
        var eligible = Math.Max(0, response.Stats.TotalFiles - response.Stats.Ignored);
        _ui.WriteKeyValue("Total", response.Stats.TotalFiles.ToString());
        _ui.WriteKeyValue("Elegiveis", eligible.ToString());
        _ui.WriteKeyValue("Mover", response.Stats.WouldMove.ToString());
        _ui.WriteKeyValue("Duplicados", response.Stats.Duplicates.ToString());
        _ui.WriteKeyValue("Ignorados", response.Stats.Ignored.ToString());
        _ui.WriteKeyValue("Erros", response.Stats.Errors.ToString());
        _ui.WriteKeyValue("Saida", response.OutputPath);
        if (eligible == 0)
            _ui.WriteWarning("Nenhum arquivo com extensoes suportadas foi encontrado.");

        var show = _input.ReadYesNo("Mostrar primeiros itens", false);
        if (show)
        {
            var limit = _input.ReadOptionalInt("Limite", "enter para 20") ?? 20;
            _ui.Section("Plano");
            foreach (var item in response.Plan.Take(limit))
                _ui.WriteLine($"{item.Action}: {item.Source} -> {item.Target}");
        }

        return response.Stats.Errors == 0 ? 0 : 1;
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
