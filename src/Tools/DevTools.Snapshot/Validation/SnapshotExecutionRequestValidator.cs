using DevTools.Core.Validation;
using DevTools.Snapshot.Models;

namespace DevTools.Snapshot.Validation;

public sealed class SnapshotExecutionRequestValidator : IValidator<SnapshotExecutionRequest>
{
    public ValidationResult Validate(SnapshotExecutionRequest instance)
    {
        var errors = new List<ValidationError>();

        if (instance is null)
        {
            return ValidationResult.Fail(new ValidationError("request", "Request nao pode ser nulo."));
        }

        if (string.IsNullOrWhiteSpace(instance.RootPath))
            errors.Add(new ValidationError("rootPath", "Caminho raiz e obrigatorio."));

        if (!HasAnyOutputEnabled(instance))
            errors.Add(new ValidationError("outputs", "Selecione pelo menos um formato de saida."));

        if (instance.MaxFileSizeKb.HasValue && instance.MaxFileSizeKb <= 0)
            errors.Add(new ValidationError("maxFileSizeKb", "MaxFileSizeKb deve ser maior que zero."));

        return errors.Count == 0 ? ValidationResult.Success : ValidationResult.Fail(errors);
    }

    private static bool HasAnyOutputEnabled(SnapshotExecutionRequest instance) =>
        instance.GenerateText ||
        instance.GenerateJsonNested ||
        instance.GenerateJsonRecursive ||
        instance.GenerateHtmlPreview;
}

