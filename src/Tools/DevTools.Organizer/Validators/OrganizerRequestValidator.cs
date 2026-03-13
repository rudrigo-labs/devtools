using DevTools.Core.Validation;
using DevTools.Organizer.Models;

namespace DevTools.Organizer.Validators;

public sealed class OrganizerRequestValidator : IValidator<OrganizerRequest>
{
    public ValidationResult Validate(OrganizerRequest instance)
    {
        if (instance is null)
            return ValidationResult.Fail(new ValidationError("request", "Request não pode ser nulo."));

        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(instance.InboxPath))
            errors.Add(new ValidationError("inboxPath", "Pasta de entrada é obrigatória."));

        if (instance.MinScore < 0)
            errors.Add(new ValidationError("minScore", "MinScore não pode ser negativo."));

        return errors.Count == 0 ? ValidationResult.Success : ValidationResult.Fail(errors);
    }
}
