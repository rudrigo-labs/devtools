using DevTools.Cli.Ui;
using DevTools.Cli.Logging;
using DevTools.Utf8Convert.Engine;
using DevTools.Utf8Convert.Models;

namespace DevTools.Cli.Commands;

public sealed class Utf8ConvertCliCommand : ICliCommand
{
    private readonly CliConsole _ui;
    private readonly CliInput _input;
    private readonly Utf8ConvertEngine _engine;

    public Utf8ConvertCliCommand(CliConsole ui, CliInput input)
    {
        _ui = ui;
        _input = input;
        _engine = new Utf8ConvertEngine();
    }

    public string Key => "utf8";
    public string Name => "Utf8 Convert";
    public string Description => "Converte textos para UTF-8 com backup e dry-run.";

    public async Task<int> ExecuteAsync(CancellationToken ct)
    {
        var root = _input.ReadRequired("Pasta raiz", "ex: C:\\Projetos\\MeuApp");
        var recursive = _input.ReadYesNo("Recursivo", true);
        var dryRun = _input.ReadYesNo("Dry-run", true);
        var backup = _input.ReadYesNo("Criar backup", true);
        var outputBom = _input.ReadYesNo("Gerar BOM", true);

        var include = _input.ReadCsv("Includes (globs)", "ex: **/*.cs, **/*.md");
        var exclude = _input.ReadCsv("Excludes (globs)", "ex: bin/**, obj/**");

        var request = new Utf8ConvertRequest(
            root,
            recursive,
            dryRun,
            backup,
            outputBom,
            include.Count == 0 ? null : include,
            exclude.Count == 0 ? null : exclude);

        using var progress = new CliProgressReporter(_ui.Theme);
        var result = await _engine.ExecuteAsync(request, progress, ct).ConfigureAwait(false);
        progress.Finish();

        if (!result.IsSuccess || result.Value is null)
        {
            WriteErrors(result.Errors);
            return 1;
        }

        var summary = result.Value.Summary;
        _ui.Section("Resumo");
        _ui.WriteKeyValue("Arquivos", summary.FilesScanned.ToString());
        _ui.WriteKeyValue("Convertidos", summary.Converted.ToString());
        _ui.WriteKeyValue("Ja UTF8", summary.AlreadyUtf8.ToString());
        _ui.WriteKeyValue("Binarios", summary.SkippedBinary.ToString());
        _ui.WriteKeyValue("Excluidos", summary.SkippedExcluded.ToString());
        _ui.WriteKeyValue("Erros", summary.Errors.ToString());

        return summary.Errors == 0 ? 0 : 1;
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
