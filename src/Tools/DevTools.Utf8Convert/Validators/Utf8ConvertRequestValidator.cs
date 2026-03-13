using DevTools.Core.Validation;
using DevTools.Utf8Convert.Models;

namespace DevTools.Utf8Convert.Validators;

public sealed class Utf8ConvertRequestValidator : IValidator<Utf8ConvertRequest>
{
    public ValidationResult Validate(Utf8ConvertRequest instance)
    {
        if (instance is null)
            return ValidationResult.Fail(new ValidationError("request", "Request não pode ser nulo."));

        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(instance.RootPath))
            errors.Add(new ValidationError("rootPath", "Pasta raiz é obrigatória."));

        if (instance.IncludeGlobs is not null && instance.IncludeGlobs.Any(s => s is null))
            errors.Add(new ValidationError("includeGlobs", "IncludeGlobs contém entradas inválidas."));

        if (instance.ExcludeGlobs is not null && instance.ExcludeGlobs.Any(s => s is null))
            errors.Add(new ValidationError("excludeGlobs", "ExcludeGlobs contém entradas inválidas."));

        return errors.Count == 0 ? ValidationResult.Success : ValidationResult.Fail(errors);
    }
}
