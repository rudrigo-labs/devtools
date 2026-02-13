using DevTools.Cli.Ui;
using DevTools.Cli.Logging;
using DevTools.Cli.App;
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

    public async Task<int> ExecuteAsync(CliLaunchOptions options, CancellationToken ct)
    {
        // 1. Resolve Parameters
        var root = options.GetOption("root") ?? options.GetOption("path");
        var recursiveStr = options.GetOption("recursive");
        var dryRunStr = options.GetOption("dry-run");
        var backupStr = options.GetOption("backup");
        var outputBomStr = options.GetOption("output-bom") ?? options.GetOption("bom");
        var includeStr = options.GetOption("include");
        var excludeStr = options.GetOption("exclude");

        bool? recursive = recursiveStr != null ? (recursiveStr == "true") : null;
        bool? dryRun = dryRunStr != null ? (dryRunStr == "true") : null;
        bool? backup = backupStr != null ? (backupStr == "true") : null;
        bool? outputBom = outputBomStr != null ? (outputBomStr == "true") : null;

        // Interactive Fallback
        if (!options.IsNonInteractive)
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                root = _input.ReadRequired("Pasta raiz", "ex: C:\\Projetos\\MeuApp");
                options.Options["root"] = root;
            }
            
            if (recursive == null)
            {
                recursive = _input.ReadYesNo("Recursivo", true);
                options.Options["recursive"] = recursive.Value.ToString().ToLower();
            }
            
            if (dryRun == null)
            {
                dryRun = _input.ReadYesNo("Dry-run", true);
                options.Options["dry-run"] = dryRun.Value.ToString().ToLower();
            }
            
            if (backup == null)
            {
                backup = _input.ReadYesNo("Criar backup", true);
                options.Options["backup"] = backup.Value.ToString().ToLower();
            }
            
            if (outputBom == null)
            {
                outputBom = _input.ReadYesNo("Gerar BOM", true);
                options.Options["output-bom"] = outputBom.Value.ToString().ToLower();
            }

            if (string.IsNullOrWhiteSpace(includeStr))
            {
                var list = _input.ReadCsv("Includes (globs)", "ex: **/*.cs, **/*.md");
                if (list.Count > 0) 
                {
                    includeStr = string.Join(",", list);
                    options.Options["include"] = includeStr;
                }
            }
            
            if (string.IsNullOrWhiteSpace(excludeStr))
            {
                var list = _input.ReadCsv("Excludes (globs)", "ex: bin/**, obj/**");
                if (list.Count > 0) 
                {
                    excludeStr = string.Join(",", list);
                    options.Options["exclude"] = excludeStr;
                }
            }
        }

        // Defaults
        recursive ??= true;
        dryRun ??= true;
        backup ??= true;
        outputBom ??= true;

        // Validation
        if (string.IsNullOrWhiteSpace(root))
        {
            _ui.WriteError("Root path required (--root).");
            return 1;
        }

        var includeList = !string.IsNullOrWhiteSpace(includeStr)
            ? includeStr.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList()
            : null;
        
        var excludeList = !string.IsNullOrWhiteSpace(excludeStr)
            ? excludeStr.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList()
            : null;

        var request = new Utf8ConvertRequest(
            root,
            recursive.Value,
            dryRun.Value,
            backup.Value,
            outputBom.Value,
            includeList,
            excludeList);

        using var progress = new CliProgressReporter(_ui.Theme);
        var result = await _engine.ExecuteAsync(request, progress, ct).ConfigureAwait(false);
        progress.Finish();

        if (!result.IsSuccess || result.Value is null)
        {
            WriteErrors(result.Errors);
            return 1;
        }

        var summary = result.Value.Summary;
        
        if (!options.IsNonInteractive)
        {
            _ui.Section("Resumo");
            _ui.WriteKeyValue("Arquivos", summary.FilesScanned.ToString());
            _ui.WriteKeyValue("Convertidos", summary.Converted.ToString());
            _ui.WriteKeyValue("Ja UTF8", summary.AlreadyUtf8.ToString());
            _ui.WriteKeyValue("Binarios", summary.SkippedBinary.ToString());
            _ui.WriteKeyValue("Excluidos", summary.SkippedExcluded.ToString());
            _ui.WriteKeyValue("Erros", summary.Errors.ToString());
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
