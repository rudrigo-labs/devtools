using DevTools.Cli.Ui;
using DevTools.Cli.Logging;
using DevTools.Snapshot.Engine;
using DevTools.Snapshot.Models;

namespace DevTools.Cli.Commands;

public sealed class SnapshotCliCommand : ICliCommand
{
    private readonly CliConsole _ui;
    private readonly CliInput _input;
    private readonly SnapshotEngine _engine;

    public SnapshotCliCommand(CliConsole ui, CliInput input)
    {
        _ui = ui;
        _input = input;
        _engine = new SnapshotEngine();
    }

    public string Key => "snapshot";
    public string Name => "Snapshot";
    public string Description => "Gera inventario de uma pasta em TXT/JSON/HTML.";

    public async Task<int> ExecuteAsync(CancellationToken ct)
    {
        var root = _input.ReadRequired("Pasta raiz", "ex: C:\\Projetos\\MeuApp");
        var outputBase = _input.ReadOptional("Saida base (opcional)", "enter para padrao");

        _ui.Section("Formatos");
        _ui.WriteLine("1) TXT");
        _ui.WriteLine("2) JSON (achatado)");
        _ui.WriteLine("3) JSON (recursivo)");
        _ui.WriteLine("4) HTML (preview)");
        _ui.WriteLine("5) Todos");

        var choice = _input.ReadInt("Escolha", 1, 5);
        var genText = choice is 1 or 5;
        var genJsonNested = choice is 2 or 5;
        var genJsonRecursive = choice is 3 or 5;
        var genHtml = choice is 4 or 5;

        var maxSize = _input.ReadOptionalInt("Max KB por arquivo", "enter para ignorar");
        var ignored = _input.ReadCsv("Ignorar pastas", "ex: bin, obj, node_modules");

        var request = new SnapshotRequest(
            root,
            string.IsNullOrWhiteSpace(outputBase) ? null : outputBase,
            genText,
            genJsonNested,
            genJsonRecursive,
            genHtml,
            ignored.Count == 0 ? null : ignored,
            maxSize);

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
        _ui.WriteKeyValue("Arquivos", response.Stats.TotalFiles.ToString());
        _ui.WriteKeyValue("Pastas", response.Stats.TotalDirectories.ToString());
        _ui.WriteKeyValue("Saida", response.OutputBasePath);

        if (response.Artifacts.Count > 0)
        {
            _ui.Section("Artefatos");
            foreach (var item in response.Artifacts)
                _ui.WriteLine($"- {item.Kind}: {item.Path}");
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
