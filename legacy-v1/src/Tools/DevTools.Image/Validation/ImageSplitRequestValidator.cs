using DevTools.Core.Abstractions;
using DevTools.Core.Results;
using DevTools.Image.Models;

namespace DevTools.Image.Validation;

public static class ImageSplitRequestValidator
{
    public static IReadOnlyList<ErrorDetail> Validate(ImageSplitRequest request, IFileSystem fs)
    {
        var errors = new List<ErrorDetail>();

        if (request is null)
        {
            errors.Add(new ErrorDetail("image.request.null", "Request is null."));
            return errors;
        }

        if (string.IsNullOrWhiteSpace(request.InputPath))
        {
            errors.Add(new ErrorDetail("image.input.required", "InputPath is required."));
        }
        else if (!fs.FileExists(request.InputPath))
        {
            errors.Add(new ErrorDetail("image.input.not_found", "Input file not found.", request.InputPath));
        }

        if (request.StartIndex < 1)
            errors.Add(new ErrorDetail("image.start_index.invalid", "StartIndex must be >= 1."));

        if (request.MinRegionWidth < 1 || request.MinRegionHeight < 1)
            errors.Add(new ErrorDetail("image.min_region.invalid", "MinRegionWidth/MinRegionHeight must be >= 1."));

        if (!string.IsNullOrWhiteSpace(request.OutputExtension) && !request.OutputExtension.StartsWith(".", StringComparison.Ordinal))
            errors.Add(new ErrorDetail("image.extension.invalid", "OutputExtension must start with '.'"));

        return errors;
    }
}
