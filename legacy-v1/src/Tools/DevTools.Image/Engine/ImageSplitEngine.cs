using DevTools.Core.Abstractions;
using DevTools.Core.Models;
using DevTools.Core.Results;
using DevTools.Image.Models;
using DevTools.Core.Providers;
using DevTools.Image.Validation;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace DevTools.Image.Engine;

public sealed class ImageSplitEngine : IDevToolEngine<ImageSplitRequest, ImageSplitResponse>
{
    private readonly IFileSystem _fs;

    public ImageSplitEngine(IFileSystem? fileSystem = null)
    {
        _fs = fileSystem ?? new SystemFileSystem();
    }

    public async Task<RunResult<ImageSplitResponse>> ExecuteAsync(
        ImageSplitRequest request,
        IProgressReporter? progress = null,
        CancellationToken ct = default)
    {
        var errors = ImageSplitRequestValidator.Validate(request, _fs);
        if (errors.Count > 0)
            return RunResult<ImageSplitResponse>.Fail(errors);

        var inputFull = Path.GetFullPath(request.InputPath);
        var outputDir = ResolveOutputDirectory(request, inputFull);
        _fs.CreateDirectory(outputDir);

        var baseName = ResolveBaseName(request, inputFull);
        var extension = ResolveExtension(request, inputFull);

        progress?.Report(new ProgressEvent("Loading image", 5, "load"));

        using var image = await SixLabors.ImageSharp.Image.LoadAsync<Rgba32>(inputFull, ct).ConfigureAwait(false);

        var splitter = new ImageSplitter { AlphaThreshold = request.AlphaThreshold };
        progress?.Report(new ProgressEvent("Detecting components", 20, "scan"));

        var components = splitter.FindConnectedComponents(image, ct)
            .Where(r => r.Width >= request.MinRegionWidth && r.Height >= request.MinRegionHeight)
            .ToList();
        var outputs = new List<ImageSplitOutput>();

        if (components.Count == 0)
        {
            var empty = new ImageSplitResponse(inputFull, outputDir, 0, outputs);
            return RunResult<ImageSplitResponse>.Success(empty);
        }

        var total = components.Count;
        for (int i = 0; i < total; i++)
        {
            ct.ThrowIfCancellationRequested();

            var rect = components[i];
            var index = request.StartIndex + i;
            var outputPath = Path.Combine(outputDir, $"{baseName}_{index}{extension}");

            progress?.Report(new ProgressEvent($"Saving {index}/{request.StartIndex + total - 1}",
                20 + (int)((i + 1) * 70.0 / total), "save"));

            if (!request.Overwrite && _fs.FileExists(outputPath))
                continue;

            using var cropped = image.Clone(ctx => ctx.Crop(new SixLabors.ImageSharp.Rectangle(rect.X, rect.Y, rect.Width, rect.Height)));
            await cropped.SaveAsync(outputPath, ct).ConfigureAwait(false);

            outputs.Add(new ImageSplitOutput(index, outputPath, rect));
        }

        progress?.Report(new ProgressEvent("Done", 100, "done"));

        var response = new ImageSplitResponse(inputFull, outputDir, components.Count, outputs);
        return RunResult<ImageSplitResponse>.Success(response);
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
