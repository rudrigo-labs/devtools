using DevTools.Core.Validation;
using DevTools.Snapshot.Models;

namespace DevTools.Snapshot.Validators;

public sealed class SnapshotConfigurationValidator : IValidator<SnapshotEntity>
{
    public ValidationResult Validate(SnapshotEntity instance)
    {
        if (instance is null)
            return ValidationResult.Fail(new ValidationError("entity", "Configuracao nao pode ser nula."));

        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(instance.Name))
            errors.Add(new ValidationError("name", "Nome e obrigatorio."));

        if (string.IsNullOrWhiteSpace(instance.RootPath))
            errors.Add(new ValidationError("rootPath", "Caminho raiz e obrigatorio."));

        return errors.Count == 0 ? ValidationResult.Success : ValidationResult.Fail(errors);
    }
}
