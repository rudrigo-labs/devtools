using DevTools.Cli.Ui;
using DevTools.Cli.Logging;
using DevTools.Rename.Engine;
using DevTools.Rename.Models;

namespace DevTools.Cli.Commands;

public sealed class RenameCliCommand : ICliCommand
{
    private readonly CliConsole _ui;
    private readonly CliInput _input;
    private readonly RenameEngine _engine;

    public RenameCliCommand(CliConsole ui, CliInput input)
    {
        _ui = ui;
        _input = input;
        _engine = new RenameEngine();
    }

    public string Key => "rename";
    public string Name => "Rename";
    public string Description => "Renomeia identificadores/arquivos C# com backup e undo.";

    public async Task<int> ExecuteAsync(CancellationToken ct)
    {
        var root = _input.ReadRequired("Pasta raiz", "ex: C:\\Projetos\\MeuApp");
        var oldText = _input.ReadRequired("Texto antigo");
        var newText = _input.ReadRequired("Texto novo");

        _ui.Section("Modo");
        _ui.WriteLine("1) Geral (identificadores C#)");
        _ui.WriteLine("2) Apenas namespace");
        var modeChoice = _input.ReadInt("Escolha", 1, 2);
        var mode = modeChoice == 2 ? RenameMode.NamespaceOnly : RenameMode.General;

        var dryRun = _input.ReadYesNo("Dry-run", true);
        var backup = _input.ReadYesNo("Criar backup", true);
        var undoLog = _input.ReadYesNo("Gerar undo log", true);

        var include = _input.ReadCsv("Includes (globs)", "ex: src/**/*.cs");
        var exclude = _input.ReadCsv("Excludes (globs)", "ex: bin/**, obj/**");

        var request = new RenameRequest(
            root,
            oldText,
            newText,
            mode,
            dryRun,
            include.Count == 0 ? null : include,
            exclude.Count == 0 ? null : exclude,
            backup,
            undoLog,
            null,
            null,
            200);

        using var progress = new CliProgressReporter(_ui.Theme);
        var result = await _engine.ExecuteAsync(request, progress, ct).ConfigureAwait(false);
        progress.Finish();

        if (!result.IsSuccess || result.Value is null)
        {
            WriteErrors(result.Errors);
            return 1;
        }

        var response = result.Value;
        var summary = response.Summary;

        _ui.Section("Resumo");
        _ui.WriteKeyValue("Arquivos", summary.FilesScanned.ToString());
        _ui.WriteKeyValue("Pastas", summary.DirectoriesScanned.ToString());
        _ui.WriteKeyValue("Atualizados", summary.FilesUpdated.ToString());
        _ui.WriteKeyValue("Renomeados", summary.FilesRenamed.ToString());
        _ui.WriteKeyValue("Dirs ren.", summary.DirectoriesRenamed.ToString());
        _ui.WriteKeyValue("Erros", summary.Errors.ToString());

        if (!string.IsNullOrWhiteSpace(response.ReportPath))
            _ui.WriteKeyValue("Relatorio", response.ReportPath);
        if (!string.IsNullOrWhiteSpace(response.UndoLogPath))
            _ui.WriteKeyValue("Undo", response.UndoLogPath);

        if (response.Changes.Count > 0)
        {
            var show = _input.ReadYesNo("Mostrar primeiras alteracoes", false);
            if (show)
            {
                var limit = _input.ReadOptionalInt("Limite", "enter para 20") ?? 20;
                _ui.Section("Alteracoes");
                foreach (var change in response.Changes.Take(limit))
                    _ui.WriteLine($"{change.Type}: {change.Path}");
            }
        }

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
