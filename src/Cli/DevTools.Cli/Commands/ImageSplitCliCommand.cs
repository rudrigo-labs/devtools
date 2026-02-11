using DevTools.Cli.Ui;
using DevTools.Cli.Logging;
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

    public async Task<int> ExecuteAsync(CancellationToken ct)
    {
        var inputPath = _input.ReadRequired("Arquivo de imagem", "ex: C:\\Projetos\\icone.png");
        var outputDir = _input.ReadOptional("Pasta de saida (opcional)", "enter para usar a pasta da imagem");
        var baseName = _input.ReadOptional("Base do nome (opcional)");
        var extension = _input.ReadOptional("Extensao (opcional)", "ex: .png");

        var advanced = _input.ReadYesNo("Configurar opcoes avancadas", false);
        byte alpha = 10;
        int startIndex = 1;
        bool overwrite = false;
        int minW = 3;
        int minH = 3;

        if (advanced)
        {
            var alphaInt = _input.ReadOptionalInt("Alpha threshold", "0-255, enter=10") ?? 10;
            alpha = (byte)Math.Clamp(alphaInt, 0, 255);
            startIndex = _input.ReadOptionalInt("Start index", "enter=1") ?? 1;
            overwrite = _input.ReadYesNo("Sobrescrever", false);
            minW = _input.ReadOptionalInt("Min largura", "enter=3") ?? 3;
            minH = _input.ReadOptionalInt("Min altura", "enter=3") ?? 3;
        }

        var request = new ImageSplitRequest(
            inputPath,
            string.IsNullOrWhiteSpace(outputDir) ? null : outputDir,
            string.IsNullOrWhiteSpace(baseName) ? null : baseName,
            string.IsNullOrWhiteSpace(extension) ? null : extension,
            alpha,
            startIndex,
            overwrite,
            minW,
            minH);

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
