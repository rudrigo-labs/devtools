using DevTools.Core.Validation;
using DevTools.Harvest.Models;

namespace DevTools.Harvest.Validators;

public sealed class HarvestRequestValidator : IValidator<HarvestRequest>
{
    public ValidationResult Validate(HarvestRequest instance)
    {
        if (instance is null)
            return ValidationResult.Fail(new ValidationError("request", "Request não pode ser nulo."));

        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(instance.RootPath))
            errors.Add(new ValidationError("rootPath", "Caminho raiz é obrigatório."));

        if (string.IsNullOrWhiteSpace(instance.OutputPath))
            errors.Add(new ValidationError("outputPath", "Pasta de destino é obrigatória."));

        if (instance.MinScore < 0)
            errors.Add(new ValidationError("minScore", "MinScore não pode ser negativo."));

        if (instance.DensityScale <= 0)
            errors.Add(new ValidationError("densityScale", "DensityScale deve ser maior que zero."));

        return errors.Count == 0 ? ValidationResult.Success : ValidationResult.Fail(errors);
    }
}
