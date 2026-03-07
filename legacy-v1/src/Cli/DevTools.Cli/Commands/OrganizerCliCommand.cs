using DevTools.Cli.Ui;
using DevTools.Cli.Logging;
using DevTools.Cli.App;
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

    public async Task<int> ExecuteAsync(CliLaunchOptions options, CancellationToken ct)
    {
        // 1. Resolve Parameters
        var inbox = options.GetOption("inbox") ?? options.GetOption("input") ?? options.GetOption("source");
        var output = options.GetOption("output") ?? options.GetOption("out") ?? options.GetOption("target");
        var config = options.GetOption("config");
        
        var minScoreStr = options.GetOption("min-score") ?? options.GetOption("min");
        int? minScore = int.TryParse(minScoreStr, out var s) ? s : null;

        var applyStr = options.GetOption("apply");
        bool? apply = applyStr != null ? (applyStr == "true") : null;

        // Interactive Fallback
        if (!options.IsNonInteractive)
        {
            if (string.IsNullOrWhiteSpace(inbox))
            {
                inbox = _input.ReadRequired("Pasta de entrada", "ex: C:\\Projetos\\Inbox");
                options.Options["inbox"] = inbox;
            }
            
            if (string.IsNullOrWhiteSpace(output))
            {
                output = _input.ReadRequired("Pasta de saida", "ex: C:\\Projetos\\Organizado");
                options.Options["output"] = output;
            }
            
            if (string.IsNullOrWhiteSpace(config))
            {
                config = _input.ReadOptional("Config (opcional)", "enter para padrao");
                if (!string.IsNullOrWhiteSpace(config)) options.Options["config"] = config;
            }
            
            if (minScore == null)
            {
                minScore = _input.ReadOptionalInt("MinScore (opcional)");
                if (minScore.HasValue) options.Options["min-score"] = minScore.Value.ToString();
            }
            
            if (apply == null)
            {
                apply = _input.ReadYesNo("Aplicar mudancas", false);
                options.Options["apply"] = apply.Value.ToString().ToLowerInvariant();
            }
        }

        // Final Validation / Defaults
        if (string.IsNullOrWhiteSpace(inbox))
        {
            _ui.WriteError("Inbox path is required (--inbox).");
            return 1;
        }
        if (string.IsNullOrWhiteSpace(output))
        {
            _ui.WriteError("Output path is required (--output).");
            return 1;
        }

        apply ??= false;

        var request = new OrganizerRequest(
            inbox,
            output,
            string.IsNullOrWhiteSpace(config) ? null : config,
            minScore,
            apply.Value);

        using var progress = new CliProgressReporter(_ui.Theme);
        var result = await _engine.ExecuteAsync(request, progress, ct).ConfigureAwait(false);
        progress.Finish();

        if (!result.IsSuccess || result.Value is null)
        {
            WriteErrors(result.Errors);
            return 1;
        }

        var response = result.Value;

        if (!options.IsNonInteractive)
        {
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
        }
        else
        {
            // Non-interactive output
            _ui.WriteLine($"TotalFiles={response.Stats.TotalFiles}");
            _ui.WriteLine($"WouldMove={response.Stats.WouldMove}");
            _ui.WriteLine($"Duplicates={response.Stats.Duplicates}");
            _ui.WriteLine($"Errors={response.Stats.Errors}");
            _ui.WriteLine($"OutputPath={response.OutputPath}");
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
