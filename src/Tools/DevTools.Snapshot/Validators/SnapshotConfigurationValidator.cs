using DevTools.Core.Models;
using DevTools.Core.Validation;
using DevTools.Snapshot.Models;

namespace DevTools.Snapshot.Validators;

public sealed class SnapshotConfigurationValidator : IValidator<SnapshotEntity>
{
    private readonly AppSettings _settings;

    public SnapshotConfigurationValidator(AppSettings? settings = null)
    {
        _settings = settings ?? new AppSettings();
    }

    public ValidationResult Validate(SnapshotEntity instance)
    {
        if (instance is null)
            return ValidationResult.Fail(new ValidationError("entity", "Configuração não pode ser nula."));

        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(instance.Name))
            errors.Add(new ValidationError("name", "Nome é obrigatório."));

        if (string.IsNullOrWhiteSpace(instance.RootPath))
            errors.Add(new ValidationError("rootPath", "Caminho raiz é obrigatório."));

        if (string.IsNullOrWhiteSpace(instance.OutputBasePath))
            errors.Add(new ValidationError("outputBasePath", "Caminho de saída é obrigatório."));

        if (!HasAnyOutputEnabled(instance))
            errors.Add(new ValidationError("outputs", "Selecione pelo menos um formato de saída."));

        if (instance.MaxFileSizeKb.HasValue)
        {
            if (instance.MaxFileSizeKb <= 0)
                errors.Add(new ValidationError("maxFileSizeKb", "MaxFileSizeKb deve ser maior que zero."));
            else if (instance.MaxFileSizeKb > _settings.FileTools.AbsoluteMaxFileSizeKb)
                errors.Add(new ValidationError("maxFileSizeKb",
                    $"MaxFileSizeKb não pode exceder {_settings.FileTools.AbsoluteMaxFileSizeKb} KB."));
        }

        return errors.Count == 0 ? ValidationResult.Success : ValidationResult.Fail(errors);
    }

    private static bool HasAnyOutputEnabled(SnapshotEntity instance) =>
        instance.GenerateText ||
        instance.GenerateJsonNested ||
        instance.GenerateJsonRecursive ||
        instance.GenerateHtmlPreview;
}
