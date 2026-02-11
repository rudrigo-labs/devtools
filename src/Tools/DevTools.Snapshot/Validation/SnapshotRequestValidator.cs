using DevTools.Core.Abstractions;
using DevTools.Core.Results;
using DevTools.Snapshot.Models;

namespace DevTools.Snapshot.Validation;

public static class SnapshotRequestValidator
{
    public static IReadOnlyList<ErrorDetail> Validate(SnapshotRequest request, IFileSystem fs)
    {
        var errors = new List<ErrorDetail>();

        if (request is null)
        {
            errors.Add(new ErrorDetail("snapshot.request.null", "Request is null."));
            return errors;
        }

        if (string.IsNullOrWhiteSpace(request.RootPath))
        {
            errors.Add(new ErrorDetail("snapshot.root.required", "RootPath is required."));
        }
        else if (!fs.DirectoryExists(request.RootPath))
        {
            errors.Add(new ErrorDetail("snapshot.root.not_found", "RootPath not found.", request.RootPath));
        }

        var anyOutput = request.GenerateText || request.GenerateJsonNested || request.GenerateJsonRecursive || request.GenerateHtmlPreview;
        if (!anyOutput)
            errors.Add(new ErrorDetail("snapshot.output.none", "At least one output format must be selected."));

        if (request.MaxFileSizeKb.HasValue && request.MaxFileSizeKb.Value <= 0)
            errors.Add(new ErrorDetail("snapshot.max_size.invalid", "MaxFileSizeKb must be greater than zero."));

        return errors;
    }
}
