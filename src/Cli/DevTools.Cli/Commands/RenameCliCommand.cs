using DevTools.Cli.Ui;
using DevTools.Cli.Logging;
using DevTools.Rename.Engine;
using DevTools.Rename.Models;
using DevTools.Cli.App;

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

    public async Task<int> ExecuteAsync(CliLaunchOptions options, CancellationToken ct)
    {
        // 1. Resolve Parameters
        var root = options.GetOption("root") ?? options.GetOption("source");
        var oldText = options.GetOption("old-text") ?? options.GetOption("old") ?? options.GetOption("from");
        var newText = options.GetOption("new-text") ?? options.GetOption("new") ?? options.GetOption("to");
        
        var modeStr = options.GetOption("mode");
        RenameMode? mode = null;
        if (modeStr != null)
        {
            if (modeStr.Equals("namespace", StringComparison.OrdinalIgnoreCase) || modeStr == "2")
                mode = RenameMode.NamespaceOnly;
            else
                mode = RenameMode.General;
        }

        var dryRunStr = options.GetOption("dry-run") ?? options.GetOption("dry");
        bool? dryRun = dryRunStr != null ? (dryRunStr == "true") : null;

        var backupStr = options.GetOption("backup");
        bool? backup = backupStr != null ? (backupStr == "true") : null;

        var undoLogStr = options.GetOption("undo-log") ?? options.GetOption("undo");
        bool? undoLog = undoLogStr != null ? (undoLogStr == "true") : null;

        var includeStr = options.GetOption("include") ?? options.GetOption("inc");
        var excludeStr = options.GetOption("exclude") ?? options.GetOption("exc");

        List<string>? include = null;
        if (!string.IsNullOrWhiteSpace(includeStr))
        {
            include = includeStr.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }

        List<string>? exclude = null;
        if (!string.IsNullOrWhiteSpace(excludeStr))
        {
            exclude = excludeStr.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }

        // Interactive Fallback
        if (!options.IsNonInteractive)
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                root = _input.ReadRequired("Pasta raiz", "ex: C:\\Projetos\\MeuApp");
                options.Options["root"] = root;
            }
            
            if (string.IsNullOrWhiteSpace(oldText))
            {
                oldText = _input.ReadRequired("Texto antigo");
                options.Options["old-text"] = oldText;
            }
            
            if (string.IsNullOrWhiteSpace(newText))
            {
                newText = _input.ReadRequired("Texto novo");
                options.Options["new-text"] = newText;
            }

            if (mode == null)
            {
                _ui.Section("Modo");
                _ui.WriteLine("1) Geral (identificadores C#)");
                _ui.WriteLine("2) Apenas namespace");
                var modeChoice = _input.ReadInt("Escolha", 1, 2);
                mode = modeChoice == 2 ? RenameMode.NamespaceOnly : RenameMode.General;
                options.Options["mode"] = modeChoice == 2 ? "namespace" : "general";
            }

            if (dryRun == null)
            {
                dryRun = _input.ReadYesNo("Dry-run", true);
                options.Options["dry-run"] = dryRun.Value.ToString().ToLowerInvariant();
            }
            
            if (backup == null)
            {
                backup = _input.ReadYesNo("Criar backup", true);
                options.Options["backup"] = backup.Value.ToString().ToLowerInvariant();
            }
            
            if (undoLog == null)
            {
                undoLog = _input.ReadYesNo("Gerar undo log", true);
                options.Options["undo-log"] = undoLog.Value.ToString().ToLowerInvariant();
            }

            if (include == null)
            {
                var list = _input.ReadCsv("Includes (globs)", "ex: src/**/*.cs");
                if (list.Count > 0) 
                {
                    include = list.ToList();
                    options.Options["include"] = string.Join(",", include);
                }
            }

            if (exclude == null)
            {
                var list = _input.ReadCsv("Excludes (globs)", "ex: bin/**, obj/**");
                if (list.Count > 0) 
                {
                    exclude = list.ToList();
                    options.Options["exclude"] = string.Join(",", exclude);
                }
            }
        }

        // Final Validation / Defaults
        if (string.IsNullOrWhiteSpace(root))
        {
            _ui.WriteError("Root path is required (--root).");
            return 1;
        }
        if (string.IsNullOrWhiteSpace(oldText))
        {
            _ui.WriteError("Old text is required (--old).");
            return 1;
        }
        if (string.IsNullOrWhiteSpace(newText))
        {
            _ui.WriteError("New text is required (--new).");
            return 1;
        }

        mode ??= RenameMode.General;
        dryRun ??= true;
        backup ??= true;
        undoLog ??= true;

        var request = new RenameRequest(
            root,
            oldText,
            newText,
            mode.Value,
            dryRun.Value,
            include?.Count > 0 ? include : null,
            exclude?.Count > 0 ? exclude : null,
            backup.Value,
            undoLog.Value,
            null,
            null,
            200);

        using var progress = new CliProgressReporter(_ui.Theme);
        var result = await _engine.ExecuteAsync(request, progress, ct).ConfigureAwait(false);
        progress.Finish();

        // Payload Display
        if (result.IsSuccess && result.Value != null)
        {
            var response = result.Value;
            var summary = response.Summary;

            if (!options.IsNonInteractive)
            {
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
            }
            else
            {
                // Non-interactive output
                _ui.WriteLine($"FilesScanned={summary.FilesScanned}");
                _ui.WriteLine($"FilesUpdated={summary.FilesUpdated}");
                _ui.WriteLine($"Errors={summary.Errors}");
                if (!string.IsNullOrWhiteSpace(response.ReportPath))
                    _ui.WriteLine($"Report={response.ReportPath}");
            }
        }

        _ui.PrintRunResult(result);
        return result.IsSuccess && result.Summary.Failed == 0 ? 0 : 1;
    }
}
