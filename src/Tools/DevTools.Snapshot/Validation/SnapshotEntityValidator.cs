using DevTools.Core.Utilities;
using DevTools.Core.Validation;
using DevTools.Snapshot.Models;

namespace DevTools.Snapshot.Validation;

public sealed class SnapshotEntityValidator : IValidator<SnapshotEntity>
{
    public ValidationResult Validate(SnapshotEntity instance)
    {
        var errors = new List<ValidationError>();

        if (instance is null)
        {
            return ValidationResult.Fail(new ValidationError("configuration", "Configuracao nao pode ser nula."));
        }

        if (string.IsNullOrWhiteSpace(instance.Name))
            errors.Add(new ValidationError("name", "Nome e obrigatorio."));

        if (!string.IsNullOrWhiteSpace(instance.Id) && !SlugNormalizer.IsValid(instance.Id))
            errors.Add(new ValidationError("id", "Id deve estar em formato slug."));

        if (string.IsNullOrWhiteSpace(instance.RootPath))
            errors.Add(new ValidationError("rootPath", "Caminho raiz e obrigatorio."));

        if (!HasAnyOutputEnabled(instance))
            errors.Add(new ValidationError("outputs", "Selecione pelo menos um formato de saida."));

        if (instance.MaxFileSizeKb.HasValue && instance.MaxFileSizeKb <= 0)
            errors.Add(new ValidationError("maxFileSizeKb", "MaxFileSizeKb deve ser maior que zero."));

        return errors.Count == 0 ? ValidationResult.Success : ValidationResult.Fail(errors);
    }

    private static bool HasAnyOutputEnabled(SnapshotEntity instance) =>
        instance.GenerateText ||
        instance.GenerateJsonNested ||
        instance.GenerateJsonRecursive ||
        instance.GenerateHtmlPreview;
}

