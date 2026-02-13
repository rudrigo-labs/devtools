using DevTools.Core.Abstractions;
using DevTools.Core.Results;
using DevTools.Harvest.Models;

namespace DevTools.Harvest.Validation;

public static class HarvestRequestValidator
{
    public static IReadOnlyList<ErrorDetail> Validate(HarvestRequest request, IFileSystem fs)
    {
        var errors = new List<ErrorDetail>();

        if (request is null)
        {
            errors.Add(new ErrorDetail("harvest.request.null", "Request is null."));
            return errors;
        }

        if (string.IsNullOrWhiteSpace(request.RootPath))
        {
            errors.Add(new ErrorDetail("harvest.root.required", "RootPath is required.", Action: "Please provide a valid source directory path."));
        }
        else if (!fs.DirectoryExists(request.RootPath))
        {
            errors.Add(new ErrorDetail("harvest.root.not_found", "RootPath not found.", Cause: request.RootPath, Action: "Ensure the directory exists and the path is correct."));
        }

        if (request.MinScore.HasValue && request.MinScore.Value < 0)
            errors.Add(new ErrorDetail("harvest.min_score.invalid", "MinScore cannot be negative.", Action: "Set MinScore to 0 or higher."));

        return errors;
    }
}
