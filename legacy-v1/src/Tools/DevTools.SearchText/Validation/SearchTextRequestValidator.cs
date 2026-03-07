using DevTools.Core.Abstractions;
using DevTools.Core.Results;
using DevTools.SearchText.Models;

namespace DevTools.SearchText.Validation;

public static class SearchTextRequestValidator
{
    public static IReadOnlyList<ErrorDetail> Validate(SearchTextRequest request, IFileSystem fs)
    {
        var errors = new List<ErrorDetail>();

        if (request is null)
        {
            errors.Add(new ErrorDetail("searchtext.request.null", "Request is null."));
            return errors;
        }

        if (string.IsNullOrWhiteSpace(request.RootPath))
        {
            errors.Add(new ErrorDetail("searchtext.root.required", "RootPath is required."));
        }
        else if (!fs.DirectoryExists(request.RootPath))
        {
            errors.Add(new ErrorDetail("searchtext.root.not_found", "RootPath not found.", request.RootPath));
        }

        if (string.IsNullOrWhiteSpace(request.Pattern))
            errors.Add(new ErrorDetail("searchtext.pattern.required", "Pattern is required."));

        if (request.MaxFileSizeKb.HasValue && request.MaxFileSizeKb.Value <= 0)
            errors.Add(new ErrorDetail("searchtext.max_size.invalid", "MaxFileSizeKb must be greater than zero."));

        if (request.MaxMatchesPerFile < 0)
            errors.Add(new ErrorDetail("searchtext.max_matches.invalid", "MaxMatchesPerFile must be >= 0."));

        return errors;
    }
}
