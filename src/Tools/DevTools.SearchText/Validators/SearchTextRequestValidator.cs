using DevTools.Core.Validation;
using DevTools.SearchText.Models;

namespace DevTools.SearchText.Validators;

public sealed class SearchTextRequestValidator : IValidator<SearchTextRequest>
{
    public ValidationResult Validate(SearchTextRequest instance)
    {
        if (instance is null)
            return ValidationResult.Fail(new ValidationError("request", "Request não pode ser nulo."));

        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(instance.RootPath))
            errors.Add(new ValidationError("rootPath", "Pasta raiz é obrigatória."));

        if (string.IsNullOrWhiteSpace(instance.Pattern))
            errors.Add(new ValidationError("pattern", "Padrão de busca é obrigatório."));

        if (instance.MaxFileSizeKb.HasValue && instance.MaxFileSizeKb.Value <= 0)
            errors.Add(new ValidationError("maxFileSizeKb", "MaxFileSizeKb deve ser maior que zero."));

        if (instance.MaxMatchesPerFile < 0)
            errors.Add(new ValidationError("maxMatchesPerFile", "MaxMatchesPerFile deve ser >= 0."));

        return errors.Count == 0 ? ValidationResult.Success : ValidationResult.Fail(errors);
    }
}
