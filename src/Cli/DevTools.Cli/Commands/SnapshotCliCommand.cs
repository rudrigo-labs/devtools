using DevTools.Cli.Ui;
using DevTools.Cli.Logging;
using DevTools.Cli.App;
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

    public async Task<int> ExecuteAsync(CliLaunchOptions options, CancellationToken ct)
    {
        // 1. Resolve Parameters
        var root = options.GetOption("root") ?? options.GetOption("source");
        var outputBase = options.GetOption("output") ?? options.GetOption("out");
        
        // Formats: txt, json-nested, json-recursive, html, all
        var formatStr = options.GetOption("format") ?? options.GetOption("formats");
        bool? genText = null;
        bool? genJsonNested = null;
        bool? genJsonRecursive = null;
        bool? genHtml = null;

        if (formatStr != null)
        {
            var formats = formatStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            genText = formats.Contains("txt") || formats.Contains("all");
            genJsonNested = formats.Contains("json") || formats.Contains("json-nested") || formats.Contains("all");
            genJsonRecursive = formats.Contains("json-recursive") || formats.Contains("all");
            genHtml = formats.Contains("html") || formats.Contains("all");
        }

        var maxSizeStr = options.GetOption("max-size") ?? options.GetOption("max");
        int? maxSize = int.TryParse(maxSizeStr, out var s) ? s : null;

        var ignoredStr = options.GetOption("ignored") ?? options.GetOption("ignore");
        List<string>? ignored = null;
        if (!string.IsNullOrWhiteSpace(ignoredStr))
        {
            ignored = ignoredStr.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }

        // Interactive Fallback
        if (!options.IsNonInteractive)
        {
            if (string.IsNullOrWhiteSpace(root))
                root = _input.ReadRequired("Pasta raiz", "ex: C:\\Projetos\\MeuApp");
            
            if (string.IsNullOrWhiteSpace(outputBase))
                outputBase = _input.ReadOptional("Saida base (opcional)", "enter para padrao");

            if (genText == null && genJsonNested == null && genJsonRecursive == null && genHtml == null)
            {
                _ui.Section("Formatos");
                _ui.WriteLine("1) TXT");
                _ui.WriteLine("2) JSON (achatado)");
                _ui.WriteLine("3) JSON (recursivo)");
                _ui.WriteLine("4) HTML (preview)");
                _ui.WriteLine("5) Todos");

                var choice = _input.ReadInt("Escolha", 1, 5);
                genText = choice is 1 or 5;
                genJsonNested = choice is 2 or 5;
                genJsonRecursive = choice is 3 or 5;
                genHtml = choice is 4 or 5;
            }

            if (maxSize == null)
                maxSize = _input.ReadOptionalInt("Max KB por arquivo", "enter para ignorar");
            
            if (ignored == null)
            {
                var list = _input.ReadCsv("Ignorar pastas", "ex: bin, obj, node_modules");
                if (list.Count > 0) ignored = list.ToList();
            }
        }

        // Final Validation / Defaults
        if (string.IsNullOrWhiteSpace(root))
        {
            _ui.WriteError("Root path is required (--root).");
            return 1;
        }

        // Default to TXT if nothing selected
        if (genText != true && genJsonNested != true && genJsonRecursive != true && genHtml != true)
        {
            genText = true;
        }
        
        genText ??= false;
        genJsonNested ??= false;
        genJsonRecursive ??= false;
        genHtml ??= false;

        var request = new SnapshotRequest(
            root,
            string.IsNullOrWhiteSpace(outputBase) ? null : outputBase,
            genText.Value,
            genJsonNested.Value,
            genJsonRecursive.Value,
            genHtml.Value,
            ignored,
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

        if (!options.IsNonInteractive)
        {
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
        }
        else
        {
            // Non-interactive output
            _ui.WriteLine($"TotalFiles={response.Stats.TotalFiles}");
            _ui.WriteLine($"TotalDirectories={response.Stats.TotalDirectories}");
            _ui.WriteLine($"OutputBasePath={response.OutputBasePath}");
            foreach (var item in response.Artifacts)
            {
                _ui.WriteLine($"Artifact:{item.Kind}={item.Path}");
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
