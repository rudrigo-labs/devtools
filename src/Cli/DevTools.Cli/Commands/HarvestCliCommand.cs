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
            {
                root = _input.ReadRequired("Pasta raiz (Source)", "ex: C:\\Projetos\\MeuApp");
                options.Options["root"] = root;
            }

            if (string.IsNullOrWhiteSpace(outputPath))
            {
                outputPath = _input.ReadRequired("Pasta de destino (Output)", "ex: C:\\Backup\\Harvest");
                options.Options["output"] = outputPath;
            }

            if (string.IsNullOrWhiteSpace(configPath))
            {
                configPath = _input.ReadOptional("Config (opcional)", "enter para usar padrao");
                if (!string.IsNullOrWhiteSpace(configPath)) options.Options["config"] = configPath;
            }

            if (minScore == null)
            {
                minScore = _input.ReadOptionalInt("MinScore (opcional)");
                if (minScore.HasValue) options.Options["min-score"] = minScore.Value.ToString();
            }

            if (copyFiles == null)
            {
                copyFiles = _input.ReadYesNo("Copiar arquivos encontrados?", true);
                options.Options["copy-files"] = copyFiles.Value.ToString().ToLowerInvariant();
            }
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

        // Payload Display (Tool Specific)
        if (result.IsSuccess && result.Value != null)
        {
            var report = result.Value.Report;
            
            if (!options.IsNonInteractive)
            {
                if (report.Hits.Count > 0)
                {
                    var showDetails = _input.ReadYesNo("Mostrar lista detalhada", false);
                    if (showDetails)
                    {
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
                }
            }
            else
            {
                // Non-Interactive (Machine Friendly)
                foreach (var hit in report.Hits)
                {
                    _ui.WriteLine($"{hit.Score:0.0}\t{hit.File}");
                }
            }
        }

        _ui.PrintRunResult(result);
        return result.IsSuccess && result.Summary.Failed == 0 ? 0 : 1;
    }
}
