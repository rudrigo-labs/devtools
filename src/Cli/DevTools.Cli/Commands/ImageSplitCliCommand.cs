using DevTools.Cli.Ui;
using DevTools.Cli.Logging;
using DevTools.Cli.App;
using DevTools.Image.Engine;
using DevTools.Image.Models;

namespace DevTools.Cli.Commands;

public sealed class ImageSplitCliCommand : ICliCommand
{
    private readonly CliConsole _ui;
    private readonly CliInput _input;
    private readonly ImageSplitEngine _engine;

    public ImageSplitCliCommand(CliConsole ui, CliInput input)
    {
        _ui = ui;
        _input = input;
        _engine = new ImageSplitEngine();
    }

    public string Key => "split";
    public string Name => "Image Split";
    public string Description => "Recorta componentes/sprites de imagem com transparencia.";

    public async Task<int> ExecuteAsync(CliLaunchOptions options, CancellationToken ct)
    {
        // 1. Resolve Parameters
        var inputPath = options.GetOption("input") ?? options.GetOption("file");
        var outputDir = options.GetOption("output") ?? options.GetOption("out");
        var baseName = options.GetOption("base-name") ?? options.GetOption("name");
        var extension = options.GetOption("extension") ?? options.GetOption("ext");
        var alphaStr = options.GetOption("alpha") ?? options.GetOption("threshold");
        var startIndexStr = options.GetOption("start-index") ?? options.GetOption("start");
        var overwriteStr = options.GetOption("overwrite");
        var minWStr = options.GetOption("min-w") ?? options.GetOption("width");
        var minHStr = options.GetOption("min-h") ?? options.GetOption("height");

        byte? alpha = byte.TryParse(alphaStr, out var a) ? a : null;
        int? startIndex = int.TryParse(startIndexStr, out var s) ? s : null;
        bool? overwrite = overwriteStr != null ? (overwriteStr == "true") : null;
        int? minW = int.TryParse(minWStr, out var w) ? w : null;
        int? minH = int.TryParse(minHStr, out var h) ? h : null;

        // Interactive Fallback
        if (!options.IsNonInteractive)
        {
            if (string.IsNullOrWhiteSpace(inputPath))
                inputPath = _input.ReadRequired("Arquivo de imagem", "ex: C:\\Projetos\\icone.png");
            
            if (string.IsNullOrWhiteSpace(outputDir))
                outputDir = _input.ReadOptional("Pasta de saida (opcional)", "enter para usar a pasta da imagem");
            
            if (string.IsNullOrWhiteSpace(baseName))
                baseName = _input.ReadOptional("Base do nome (opcional)");
            
            if (string.IsNullOrWhiteSpace(extension))
                extension = _input.ReadOptional("Extensao (opcional)", "ex: .png");

            // Advanced options usually asked together
            bool askAdvanced = alpha == null && startIndex == null && overwrite == null && minW == null && minH == null;
            if (askAdvanced)
            {
                var advanced = _input.ReadYesNo("Configurar opcoes avancadas", false);
                if (advanced)
                {
                    var alphaInt = _input.ReadOptionalInt("Alpha threshold", "0-255, enter=10") ?? 10;
                    alpha = (byte)Math.Clamp(alphaInt, 0, 255);
                    startIndex = _input.ReadOptionalInt("Start index", "enter=1") ?? 1;
                    overwrite = _input.ReadYesNo("Sobrescrever", false);
                    minW = _input.ReadOptionalInt("Min largura", "enter=3") ?? 3;
                    minH = _input.ReadOptionalInt("Min altura", "enter=3") ?? 3;
                }
            }
        }

        // Defaults
        alpha ??= 10;
        startIndex ??= 1;
        overwrite ??= false;
        minW ??= 3;
        minH ??= 3;

        // Validation
        if (string.IsNullOrWhiteSpace(inputPath))
        {
            _ui.WriteError("Input file required (--input).");
            return 1;
        }

        var request = new ImageSplitRequest(
            inputPath,
            string.IsNullOrWhiteSpace(outputDir) ? null : outputDir,
            string.IsNullOrWhiteSpace(baseName) ? null : baseName,
            string.IsNullOrWhiteSpace(extension) ? null : extension,
            alpha.Value,
            startIndex.Value,
            overwrite.Value,
            minW.Value,
            minH.Value);

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
            _ui.WriteKeyValue("Entrada", response.InputPath);
            _ui.WriteKeyValue("Saida", response.OutputDirectory);
            _ui.WriteKeyValue("Total", response.TotalComponents.ToString());

            if (response.Outputs.Count > 0)
            {
                var show = _input.ReadYesNo("Mostrar arquivos gerados", false);
                if (show)
                {
                    _ui.Section("Arquivos");
                    foreach (var item in response.Outputs)
                        _ui.WriteLine($"{item.Index}: {item.Path}");
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
