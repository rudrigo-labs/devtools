using DevTools.Cli.Ui;
using DevTools.Cli.Logging;
using DevTools.Harvest.Engine;
using DevTools.Harvest.Models;

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

    public async Task<int> ExecuteAsync(CancellationToken ct)
    {
        var root = _input.ReadRequired("Pasta raiz (Source)", "ex: C:\\Projetos\\MeuApp");
        var outputPath = _input.ReadRequired("Pasta de destino (Output)", "ex: C:\\Backup\\Harvest");
        var configPath = _input.ReadOptional("Config (opcional)", "enter para usar padrao");
        var minScore = _input.ReadOptionalInt("MinScore (opcional)");
        var copyFiles = _input.ReadYesNo("Copiar arquivos encontrados?", true);

        var request = new HarvestRequest(
            root,
            outputPath,
            string.IsNullOrWhiteSpace(configPath) ? null : configPath,
            minScore,
            copyFiles);

        using var progress = new CliProgressReporter(_ui.Theme);
        var result = await _engine.ExecuteAsync(request, progress, ct).ConfigureAwait(false);
        progress.Finish();

        if (!result.IsSuccess || result.Value is null)
        {
            WriteErrors(result.Errors);
            return 1;
        }

        var report = result.Value.Report;
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
