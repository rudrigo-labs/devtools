using DevTools.Core.Results;
using DevTools.Utf8Convert.Models;

namespace DevTools.Utf8Convert.Validation;

public static class Utf8ConvertRequestValidator
{
    public static IReadOnlyList<ErrorDetail> Validate(Utf8ConvertRequest request)
    {
        var errors = new List<ErrorDetail>();

        if (request is null)
        {
            errors.Add(new ErrorDetail("utf8.request.null", "Request is null."));
            return errors;
        }

        if (string.IsNullOrWhiteSpace(request.RootPath))
        {
            errors.Add(new ErrorDetail("utf8.root.required", "RootPath is required."));
        }
        else if (!Directory.Exists(request.RootPath))
        {
            errors.Add(new ErrorDetail("utf8.root.not_found", "RootPath not found.", request.RootPath));
        }

        if (request.IncludeGlobs is not null && request.IncludeGlobs.Any(s => s is null))
            errors.Add(new ErrorDetail("utf8.include.invalid", "IncludeGlobs contains invalid entries."));

        if (request.ExcludeGlobs is not null && request.ExcludeGlobs.Any(s => s is null))
            errors.Add(new ErrorDetail("utf8.exclude.invalid", "ExcludeGlobs contains invalid entries."));

        return errors;
    }
}
