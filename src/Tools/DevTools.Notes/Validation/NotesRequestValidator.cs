using DevTools.Core.Results;
using DevTools.Notes.Models;

namespace DevTools.Notes.Validation;

public static class NotesRequestValidator
{
    public static IReadOnlyList<ErrorDetail> Validate(NotesRequest request)
    {
        var errors = new List<ErrorDetail>();

        if (request is null)
        {
            errors.Add(new ErrorDetail("notes.request.null", "Request is null."));
            return errors;
        }

        if (!Enum.IsDefined(typeof(NotesAction), request.Action))
        {
            errors.Add(new ErrorDetail("notes.action.invalid", "Action is invalid."));
            return errors;
        }

        if (request.Action is NotesAction.LoadNote or NotesAction.SaveNote)
        {
            if (string.IsNullOrWhiteSpace(request.NoteKey))
                errors.Add(new ErrorDetail("notes.key.required", "NoteKey is required."));
        }

        if (request.Action == NotesAction.SaveNote && request.Content is null)
            errors.Add(new ErrorDetail("notes.content.required", "Content is required for SaveNote."));

        if (request.Action == NotesAction.CreateItem)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                errors.Add(new ErrorDetail("notes.title.required", "Title is required for CreateItem."));
            if (request.Content is null)
                errors.Add(new ErrorDetail("notes.content.required", "Content is required for CreateItem."));
        }

        if (request.Action == NotesAction.ImportZip)
        {
            if (string.IsNullOrWhiteSpace(request.ZipPath))
                errors.Add(new ErrorDetail("notes.zip.required", "ZipPath is required for ImportZip."));
        }

        if (!string.IsNullOrWhiteSpace(request.NotesRootPath))
        {
            var trimmed = request.NotesRootPath.Trim();
            if (trimmed.Length == 0)
                errors.Add(new ErrorDetail("notes.root.invalid", "NotesRootPath is invalid."));
        }

        return errors;
    }
}
