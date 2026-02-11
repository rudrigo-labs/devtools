using DevTools.Cli.Ui;
using DevTools.Cli.Logging;
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

    public async Task<int> ExecuteAsync(CancellationToken ct)
    {
        var root = _input.ReadRequired("Pasta raiz", "ex: C:\\Projetos\\MeuApp");
        var pattern = _input.ReadRequired("Texto ou regex");
        var useRegex = _input.ReadYesNo("Usar regex", false);
        var caseSensitive = _input.ReadYesNo("Diferenciar maiusculas", false);
        var wholeWord = _input.ReadYesNo("Palavra inteira", false);

        var include = _input.ReadCsv("Includes (globs)", "ex: src/**/*.cs, **/*.md");
        var exclude = _input.ReadCsv("Excludes (globs)", "ex: bin/**, obj/**");
        var maxSize = _input.ReadOptionalInt("Max KB por arquivo", "enter para ignorar");
        var skipBinary = _input.ReadYesNo("Ignorar binarios", true);
        var maxPerFile = _input.ReadOptionalInt("Max matches por arquivo", "0 = sem limite") ?? 0;
        var returnLines = _input.ReadYesNo("Mostrar linhas", true);

        var request = new SearchTextRequest(
            root,
            pattern,
            useRegex,
            caseSensitive,
            wholeWord,
            include.Count == 0 ? null : include,
            exclude.Count == 0 ? null : exclude,
            maxSize,
            skipBinary,
            maxPerFile,
            returnLines);

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
        _ui.WriteKeyValue("Arquivos", response.TotalFilesScanned.ToString());
        _ui.WriteKeyValue("Com match", response.TotalFilesWithMatches.ToString());
        _ui.WriteKeyValue("Ocorrencias", response.TotalOccurrences.ToString());

        if (response.TotalFilesWithMatches == 0)
            return 0;

        var showDetails = _input.ReadYesNo("Mostrar detalhes", true);
        if (!showDetails)
            return 0;

        _ui.Section("Resultados");
        foreach (var file in response.Files.OrderBy(f => f.RelativePath))
        {
            _ui.WriteLine($"{file.RelativePath} | {file.Occurrences}");
            if (!returnLines)
                continue;

            foreach (var line in file.Lines)
            {
                var cols = string.Join(",", line.Columns);
                _ui.WriteDim($"  {line.LineNumber}:{cols}  {line.LineText}");
            }
            _ui.WriteLine();
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
