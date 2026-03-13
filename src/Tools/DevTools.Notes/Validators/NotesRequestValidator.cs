using DevTools.Core.Results;
using DevTools.Core.Validation;
using DevTools.Notes.Models;

namespace DevTools.Notes.Validators;

public sealed class NotesRequestValidator : IValidator<NotesRequest>
{
    public ValidationResult Validate(NotesRequest instance)
    {
        if (instance is null)
            return ValidationResult.Fail(new ValidationError("request", "Request não pode ser nulo."));

        var errors = new List<ValidationError>();

        if (!Enum.IsDefined(typeof(NotesAction), instance.Action))
        {
            errors.Add(new ValidationError("action", "Action inválida."));
            return ValidationResult.Fail(errors);
        }

        if (instance.Action is NotesAction.LoadNote or NotesAction.SaveNote or NotesAction.DeleteItem)
        {
            if (string.IsNullOrWhiteSpace(instance.NoteKey))
                errors.Add(new ValidationError("noteKey", "NoteKey é obrigatório."));
        }

        if (instance.Action == NotesAction.SaveNote && instance.Content is null)
            errors.Add(new ValidationError("content", "Content é obrigatório para SaveNote."));

        if (instance.Action == NotesAction.CreateItem)
        {
            if (string.IsNullOrWhiteSpace(instance.Title))
                errors.Add(new ValidationError("title", "Title é obrigatório para CreateItem."));

            if (instance.Content is null)
                errors.Add(new ValidationError("content", "Content é obrigatório para CreateItem."));

            var ext = instance.Extension?.Trim().ToLowerInvariant();
            if (ext is not null && ext != ".md" && ext != ".txt")
                errors.Add(new ValidationError("extension", "Extension deve ser .md ou .txt."));
        }

        if (instance.Action == NotesAction.ImportZip)
        {
            if (string.IsNullOrWhiteSpace(instance.ZipPath))
                errors.Add(new ValidationError("zipPath", "ZipPath é obrigatório para ImportZip."));
        }

        return errors.Count == 0 ? ValidationResult.Success : ValidationResult.Fail(errors);
    }

    // Compatibilidade com o código legado do Engine que chama Validate(request) retornando lista
    internal static IReadOnlyList<ErrorDetail> ValidateLegacy(NotesRequest request)
    {
        var validator = new NotesRequestValidator();
        var result = validator.Validate(request);
        if (result.IsValid)
            return Array.Empty<ErrorDetail>();

        return result.Errors
            .Select(e => new ErrorDetail($"notes.{e.Field}", e.Message))
            .ToList();
    }
}
