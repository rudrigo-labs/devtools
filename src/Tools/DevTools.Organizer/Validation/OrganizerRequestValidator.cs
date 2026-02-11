using DevTools.Core.Abstractions;
using DevTools.Core.Results;
using DevTools.Organizer.Models;

namespace DevTools.Organizer.Validation;

public static class OrganizerRequestValidator
{
    public static IReadOnlyList<ErrorDetail> Validate(OrganizerRequest request, IFileSystem fs)
    {
        var errors = new List<ErrorDetail>();

        if (request is null)
        {
            errors.Add(new ErrorDetail("organizer.request.null", "Request is null."));
            return errors;
        }

        if (string.IsNullOrWhiteSpace(request.InboxPath))
        {
            errors.Add(new ErrorDetail("organizer.inbox.required", "InboxPath is required."));
        }
        else if (!fs.DirectoryExists(request.InboxPath))
        {
            errors.Add(new ErrorDetail("organizer.inbox.not_found", "InboxPath not found.", request.InboxPath));
        }

        if (string.IsNullOrWhiteSpace(request.OutputPath))
        {
            errors.Add(new ErrorDetail("organizer.output.required", "OutputPath is required."));
        }

        if (request.MinScore.HasValue && request.MinScore.Value < 0)
            errors.Add(new ErrorDetail("organizer.min_score.invalid", "MinScore cannot be negative."));

        return errors;
    }
}
