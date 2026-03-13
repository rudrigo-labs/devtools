using DevTools.Core.Abstractions;
using DevTools.Core.Models;
using DevTools.Core.Results;
using DevTools.Core.Validation;
using DevTools.Image.Models;
using DevTools.Image.Validators;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace DevTools.Image.Engine;

public sealed class ImageSplitEngine : IDevToolEngine<ImageSplitRequest, ImageSplitResult>
{
    private readonly IValidator<ImageSplitRequest> _validator;
    private readonly ImageSplitter _splitter = new();

    public ImageSplitEngine(IValidator<ImageSplitRequest>? validator = null)
    {
        _validator = validator ?? new ImageSplitRequestValidator();
    }

    public async Task<RunResult<ImageSplitResult>> ExecuteAsync(
        ImageSplitRequest request,
        IProgressReporter? progress = null,
        CancellationToken cancellationToken = default)
    {
        var validation = _validator.Validate(request);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .Select(e => new ErrorDetail($"image.{e.Field}", e.Message))
                .ToList();
            return RunResult<ImageSplitResult>.Fail(errors);
        }

        if (!File.Exists(request.InputPath))
        {
            return RunResult<ImageSplitResult>.Fail(new ErrorDetail(
                "image.input.not_found",
                "Arquivo de entrada não encontrado.",
                Cause: request.InputPath));
        }

        var inputFull = Path.GetFullPath(request.InputPath);
        var outputDir = ResolveOutputDirectory(request, inputFull);

        try { Directory.CreateDirectory(outputDir); }
        catch (Exception ex)
        {
            return RunResult<ImageSplitResult>.Fail(new ErrorDetail(
                "image.output.create_dir_error",
                "Falha ao criar pasta de saída.",
                Cause: outputDir,
                Exception: ex));
        }

        var baseName = ResolveBaseName(request, inputFull);
        var extension = ResolveExtension(request, inputFull);

        progress?.Report(new ProgressEvent("Carregando imagem", 5, "load"));

        Image<Rgba32> image;
        try
        {
            image = await SixLabors.ImageSharp.Image
                .LoadAsync<Rgba32>(inputFull, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return RunResult<ImageSplitResult>.Fail(new ErrorDetail(
                "image.load_error",
                "Falha ao carregar imagem. Verifique se o arquivo é uma imagem válida.",
                Cause: inputFull,
                Exception: ex));
        }

        using (image)
        {
            _splitter.AlphaThreshold = request.AlphaThreshold;

            progress?.Report(new ProgressEvent("Detectando componentes", 20, "scan"));

            var components = _splitter
                .FindConnectedComponents(image, cancellationToken)
                .Where(r => r.Width >= request.MinRegionWidth && r.Height >= request.MinRegionHeight)
                .ToList();

            if (components.Count == 0)
            {
                var empty = new ImageSplitResult(inputFull, outputDir, 0, []);
                return RunResult<ImageSplitResult>.Success(empty);
            }

            var outputs = new List<ImageSplitOutput>(components.Count);
            var total = components.Count;

            for (int i = 0; i < total; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var rect = components[i];
                var index = request.StartIndex + i;
                var outputPath = Path.Combine(outputDir, $"{baseName}_{index}{extension}");

                progress?.Report(new ProgressEvent(
                    $"Salvando {index}/{request.StartIndex + total - 1}",
                    20 + (int)((i + 1) * 75.0 / total),
                    "save"));

                if (!request.Overwrite && File.Exists(outputPath))
                    continue;

                using var cropped = image.Clone(ctx =>
                    ctx.Crop(new Rectangle(rect.X, rect.Y, rect.Width, rect.Height)));

                await cropped.SaveAsync(outputPath, cancellationToken).ConfigureAwait(false);

                outputs.Add(new ImageSplitOutput(index, outputPath, rect));
            }

            progress?.Report(new ProgressEvent("Concluído", 100, "done"));

            var summary = new RunSummary(
                ToolName: "ImageSplit",
                Mode: "Real",
                MainInput: inputFull,
                OutputLocation: outputDir,
                Processed: total,
                Changed: outputs.Count,
                Ignored: total - outputs.Count,
                Failed: 0,
                Duration: TimeSpan.Zero);

            var result = new ImageSplitResult(inputFull, outputDir, total, outputs);
            return RunResult<ImageSplitResult>.Success(result).WithSummary(summary);
        }
    }

    private static string ResolveOutputDirectory(ImageSplitRequest request, string inputFull)
    {
        if (!string.IsNullOrWhiteSpace(request.OutputDirectory))
            return Path.GetFullPath(request.OutputDirectory);

        var dir = Path.GetDirectoryName(inputFull);
        return string.IsNullOrWhiteSpace(dir) ? Directory.GetCurrentDirectory() : dir;
    }

    private static string ResolveBaseName(ImageSplitRequest request, string inputFull)
    {
        if (!string.IsNullOrWhiteSpace(request.OutputBaseName))
            return request.OutputBaseName.Trim();

        return Path.GetFileNameWithoutExtension(inputFull);
    }

    private static string ResolveExtension(ImageSplitRequest request, string inputFull)
    {
        if (!string.IsNullOrWhiteSpace(request.OutputExtension))
            return request.OutputExtension;

        var ext = Path.GetExtension(inputFull);
        return string.IsNullOrWhiteSpace(ext) ? ".png" : ext;
    }
}
