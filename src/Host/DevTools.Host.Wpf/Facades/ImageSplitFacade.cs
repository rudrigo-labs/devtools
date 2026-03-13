using DevTools.Core.Results;
using DevTools.Image.Engine;
using DevTools.Image.Models;

namespace DevTools.Host.Wpf.Facades;

public sealed class ImageSplitFacade : IImageSplitFacade
{
    private readonly ImageSplitEngine _engine;

    public ImageSplitFacade(ImageSplitEngine engine)
    {
        _engine = engine;
    }

    public Task<RunResult<ImageSplitResult>> ExecuteAsync(ImageSplitRequest request, CancellationToken ct = default) =>
        _engine.ExecuteAsync(request, cancellationToken: ct);
}
