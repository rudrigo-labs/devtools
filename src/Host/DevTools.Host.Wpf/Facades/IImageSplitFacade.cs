using DevTools.Core.Results;
using DevTools.Image.Models;

namespace DevTools.Host.Wpf.Facades;

public interface IImageSplitFacade
{
    Task<RunResult<ImageSplitResult>> ExecuteAsync(ImageSplitRequest request, CancellationToken ct = default);
}
