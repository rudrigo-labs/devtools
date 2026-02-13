using DevTools.Cli.Ui;
using DevTools.Cli.Logging;
using DevTools.Cli.App;
using DevTools.SearchText.Engine;
using DevTools.SearchText.Models;

namespace DevTools.Cli.Commands;

public sealed class SearchTextCliCommand : ICliCommand
{
    private readonly CliConsole _ui;
    private readonly CliInput _input;
    private readonly SearchTextEngine _engine;

    public SearchTextCliCommand(CliConsole ui, CliInput input)
    {
        _ui = ui;
        _input = input;
        _engine = new SearchTextEngine();
    }

    public string Key => "searchtext";
    public string Name => "Search Text";
    public string Description => "Busca texto ou regex em arquivos com filtros.";

    public async Task<int> ExecuteAsync(CliLaunchOptions options, CancellationToken ct)
    {
        // 1. Resolve Parameters
        var root = options.GetOption("root") ?? options.GetOption("path");
        var pattern = options.GetOption("pattern") ?? options.GetOption("text");
        var regexStr = options.GetOption("regex");
        var caseSensitiveStr = options.GetOption("case-sensitive") ?? options.GetOption("case");
        var wholeWordStr = options.GetOption("whole-word") ?? options.GetOption("word");
        var includeStr = options.GetOption("include");
        var excludeStr = options.GetOption("exclude");
        var maxSizeStr = options.GetOption("max-size") ?? options.GetOption("size");
        var skipBinaryStr = options.GetOption("skip-binary") ?? options.GetOption("binary");
        var maxPerFileStr = options.GetOption("max-per-file");
        var showLinesStr = options.GetOption("show-lines") ?? options.GetOption("lines");

        bool? regex = regexStr != null ? (regexStr == "true") : null;
        bool? caseSensitive = caseSensitiveStr != null ? (caseSensitiveStr == "true") : null;
        bool? wholeWord = wholeWordStr != null ? (wholeWordStr == "true") : null;
        bool? skipBinary = skipBinaryStr != null ? (skipBinaryStr == "true") : null;
        bool? showLines = showLinesStr != null ? (showLinesStr == "true") : null;
        int? maxSize = int.TryParse(maxSizeStr, out var s) ? s : null;
        int? maxPerFile = int.TryParse(maxPerFileStr, out var m) ? m : null;

        // Interactive Fallback
        if (!options.IsNonInteractive)
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                root = _input.ReadRequired("Pasta raiz", "ex: C:\\Projetos\\MeuApp");
                options.Options["root"] = root;
            }
            
            if (string.IsNullOrWhiteSpace(pattern))
            {
                pattern = _input.ReadRequired("Texto ou regex");
                options.Options["pattern"] = pattern;
            }
            
            if (regex == null)
            {
                regex = _input.ReadYesNo("Usar regex", false);
                options.Options["regex"] = regex.Value.ToString().ToLower();
            }
            
            if (caseSensitive == null)
            {
                caseSensitive = _input.ReadYesNo("Diferenciar maiusculas", false);
                options.Options["case-sensitive"] = caseSensitive.Value.ToString().ToLower();
            }
            
            if (wholeWord == null)
            {
                wholeWord = _input.ReadYesNo("Palavra inteira", false);
                options.Options["whole-word"] = wholeWord.Value.ToString().ToLower();
            }

            if (string.IsNullOrWhiteSpace(includeStr))
            {
                var list = _input.ReadCsv("Includes (globs)", "ex: src/**/*.cs, **/*.md");
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

            if (maxSize == null)
            {
                maxSize = _input.ReadOptionalInt("Max KB por arquivo", "enter para ignorar");
                if (maxSize.HasValue) options.Options["max-size"] = maxSize.Value.ToString();
            }
            
            if (skipBinary == null)
            {
                skipBinary = _input.ReadYesNo("Ignorar binarios", true);
                options.Options["skip-binary"] = skipBinary.Value.ToString().ToLower();
            }
            
            if (maxPerFile == null)
            {
                maxPerFile = _input.ReadOptionalInt("Max matches por arquivo", "0 = sem limite") ?? 0;
                options.Options["max-per-file"] = maxPerFile.Value.ToString();
            }
            
            if (showLines == null)
            {
                showLines = _input.ReadYesNo("Mostrar linhas", true);
                options.Options["show-lines"] = showLines.Value.ToString().ToLower();
            }
        }

        // Defaults
        regex ??= false;
        caseSensitive ??= false;
        wholeWord ??= false;
        skipBinary ??= true;
        maxPerFile ??= 0;
        showLines ??= true;

        // Validation
        if (string.IsNullOrWhiteSpace(root))
        {
            _ui.WriteError("Root path required (--root).");
            return 1;
        }
        if (string.IsNullOrWhiteSpace(pattern))
        {
            _ui.WriteError("Pattern required (--pattern).");
            return 1;
        }

        var includeList = !string.IsNullOrWhiteSpace(includeStr)
            ? includeStr.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList()
            : null;
        
        var excludeList = !string.IsNullOrWhiteSpace(excludeStr)
            ? excludeStr.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList()
            : null;

        var request = new SearchTextRequest(
            root,
            pattern,
            regex.Value,
            caseSensitive.Value,
            wholeWord.Value,
            includeList,
            excludeList,
            maxSize,
            skipBinary.Value,
            maxPerFile.Value,
            showLines.Value);

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
            _ui.WriteKeyValue("Arquivos", response.TotalFilesScanned.ToString());
            _ui.WriteKeyValue("Com match", response.TotalFilesWithMatches.ToString());
            _ui.WriteKeyValue("Ocorrencias", response.TotalOccurrences.ToString());

            if (response.TotalFilesWithMatches > 0)
            {
                var showDetails = _input.ReadYesNo("Mostrar detalhes", true);
                if (showDetails)
                {
                    _ui.Section("Resultados");
                    foreach (var file in response.Files.OrderBy(f => f.RelativePath))
                    {
                        _ui.WriteLine($"{file.RelativePath} | {file.Occurrences}");
                        if (!showLines.Value)
                            continue;

                        foreach (var line in file.Lines)
                        {
                            var cols = string.Join(",", line.Columns);
                            _ui.WriteDim($"  {line.LineNumber}:{cols}  {line.LineText}");
                        }
                        _ui.WriteLine();
                    }
                }
            }
        }
        else
        {
            // In non-interactive mode, output results if matches found
             if (response.TotalFilesWithMatches > 0)
            {
                foreach (var file in response.Files.OrderBy(f => f.RelativePath))
                {
                    Console.WriteLine($"{file.RelativePath}:{file.Occurrences}"); // Simple format for piping
                    if (showLines.Value)
                    {
                        foreach (var line in file.Lines)
                        {
                            var cols = string.Join(",", line.Columns);
                            Console.WriteLine($"  {line.LineNumber}:{cols}  {line.LineText}");
                        }
                    }
                }
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
