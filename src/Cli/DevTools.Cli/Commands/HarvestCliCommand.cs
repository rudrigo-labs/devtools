using DevTools.Cli.Ui;
using DevTools.Cli.Logging;
using DevTools.Harvest.Engine;
using DevTools.Harvest.Models;
using DevTools.Cli.App;

namespace DevTools.Cli.Commands;

public sealed class HarvestCliCommand : ICliCommand
{
    private readonly CliConsole _ui;
    private readonly CliInput _input;
    private readonly HarvestEngine _engine;

    public HarvestCliCommand(CliConsole ui, CliInput input)
    {
        _ui = ui;
        _input = input;
        _engine = new HarvestEngine();
    }

    public string Key => "harvest";
    public string Name => "Harvest";
    public string Description => "Identifica classes C# reutilizaveis (helpers/extensions).";

    public async Task<int> ExecuteAsync(CliLaunchOptions options, CancellationToken ct)
    {
        // 1. Resolve Parameters (Args -> Interactive -> Default/Error)
        var root = options.GetOption("root") ?? options.GetOption("source");
        var outputPath = options.GetOption("output") ?? options.GetOption("out");
        var configPath = options.GetOption("config");
        
        var minScoreStr = options.GetOption("min-score") ?? options.GetOption("min");
        int? minScore = int.TryParse(minScoreStr, out var s) ? s : null;

        var copyFilesStr = options.GetOption("copy-files") ?? options.GetOption("copy");
        bool? copyFiles = copyFilesStr != null ? (copyFilesStr == "true") : null;

        // Interactive Fallback
        if (!options.IsNonInteractive)
        {
            if (string.IsNullOrWhiteSpace(root))
                root = _input.ReadRequired("Pasta raiz (Source)", "ex: C:\\Projetos\\MeuApp");

            if (string.IsNullOrWhiteSpace(outputPath))
                outputPath = _input.ReadRequired("Pasta de destino (Output)", "ex: C:\\Backup\\Harvest");

            if (string.IsNullOrWhiteSpace(configPath))
                configPath = _input.ReadOptional("Config (opcional)", "enter para usar padrao");

            if (minScore == null)
                minScore = _input.ReadOptionalInt("MinScore (opcional)");

            if (copyFiles == null)
                copyFiles = _input.ReadYesNo("Copiar arquivos encontrados?", true);
        }

        // Final Validation / Defaults
        if (string.IsNullOrWhiteSpace(root))
        {
            _ui.WriteError("Root path is required (--root or --source).");
            return 1;
        }
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            _ui.WriteError("Output path is required (--output or --out).");
            return 1;
        }

        // Default to true if not specified (matches original interactive default)
        copyFiles ??= true;

        var request = new HarvestRequest(
            root,
            outputPath,
            string.IsNullOrWhiteSpace(configPath) ? null : configPath,
            minScore,
            copyFiles.Value);

        using var progress = new CliProgressReporter(_ui.Theme);
        var result = await _engine.ExecuteAsync(request, progress, ct).ConfigureAwait(false);
        progress.Finish();

        if (!result.IsSuccess || result.Value is null)
        {
            WriteErrors(result.Errors);
            return 1;
        }

        var report = result.Value.Report;

        // Interactive Output
        if (!options.IsNonInteractive)
        {
            _ui.Section("Resumo");
            _ui.WriteKeyValue("Arquivos", report.TotalFilesAnalyzed.ToString());
            _ui.WriteKeyValue("Pontuados", report.TotalFilesScored.ToString());
            _ui.WriteKeyValue("Hits", report.Hits.Count.ToString());

            if (report.Issues.Count > 0)
            {
                _ui.Section("Avisos");
                foreach (var issue in report.Issues.Take(5))
                    _ui.WriteWarning($"{issue.Code}: {issue.Message}");
            }

            if (report.Hits.Count == 0)
                return 0;

            var showDetails = _input.ReadYesNo("Mostrar lista detalhada", false);
            if (!showDetails)
                return 0;

            var limit = _input.ReadOptionalInt("Limite de itens", "enter para todos") ?? report.Hits.Count;
            _ui.Section("Top Hits");
            foreach (var hit in report.Hits.Take(limit))
            {
                _ui.WriteLine($"{hit.Score,6:0.0} | {hit.File}");
                if (hit.Tags.Count > 0)
                    _ui.WriteDim($"  Tags: {string.Join(", ", hit.Tags)}");
                if (hit.Reasons.Count > 0)
                    _ui.WriteDim($"  Motivos: {string.Join(" | ", hit.Reasons.Take(3))}");
            }
        }
        else
        {
            // Non-Interactive Output (Machine Friendly)
            foreach (var hit in report.Hits)
            {
                _ui.WriteLine($"{hit.Score:0.0}\t{hit.File}");
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
