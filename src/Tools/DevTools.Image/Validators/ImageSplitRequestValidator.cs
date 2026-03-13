using DevTools.Core.Validation;
using DevTools.Image.Models;

namespace DevTools.Image.Validators;

public sealed class ImageSplitRequestValidator : IValidator<ImageSplitRequest>
{
    public ValidationResult Validate(ImageSplitRequest instance)
    {
        if (instance is null)
            return ValidationResult.Fail(new ValidationError("request", "Request não pode ser nulo."));

        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(instance.InputPath))
            errors.Add(new ValidationError("inputPath", "Arquivo de entrada é obrigatório."));

        if (string.IsNullOrWhiteSpace(instance.OutputDirectory))
            errors.Add(new ValidationError("outputDirectory", "Pasta de saída é obrigatória."));

        if (instance.StartIndex < 1)
            errors.Add(new ValidationError("startIndex", "StartIndex deve ser maior ou igual a 1."));

        if (instance.MinRegionWidth < 1)
            errors.Add(new ValidationError("minRegionWidth", "MinRegionWidth deve ser maior ou igual a 1."));

        if (instance.MinRegionHeight < 1)
            errors.Add(new ValidationError("minRegionHeight", "MinRegionHeight deve ser maior ou igual a 1."));

        if (!string.IsNullOrWhiteSpace(instance.OutputExtension)
            && !instance.OutputExtension.StartsWith(".", StringComparison.Ordinal))
            errors.Add(new ValidationError("outputExtension", "OutputExtension deve começar com '.'"));

        return errors.Count == 0 ? ValidationResult.Success : ValidationResult.Fail(errors);
    }
}
