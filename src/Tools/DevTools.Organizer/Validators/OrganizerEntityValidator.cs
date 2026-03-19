using DevTools.Core.Validation;
using DevTools.Organizer.Models;

namespace DevTools.Organizer.Validators;

public sealed class OrganizerEntityValidator : IValidator<OrganizerEntity>
{
    public ValidationResult Validate(OrganizerEntity instance)
    {
        if (instance is null)
            return ValidationResult.Fail(new ValidationError("request", "Configuração não pode ser nula."));

        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(instance.Name))
            errors.Add(new ValidationError("name", "Nome da configuração é obrigatório."));

        if (instance.MinScore < 0)
            errors.Add(new ValidationError("minScore", "MinScore não pode ser negativo."));

        if (instance.DeduplicateFirstLines < 0)
            errors.Add(new ValidationError("deduplicateFirstLines", "DeduplicateFirstLines não pode ser negativo."));

        if (string.IsNullOrWhiteSpace(instance.DuplicatesFolderName))
            errors.Add(new ValidationError("duplicatesFolderName", "Nome da pasta de duplicatas é obrigatório."));

        if (string.IsNullOrWhiteSpace(instance.OthersFolderName))
            errors.Add(new ValidationError("othersFolderName", "Nome da pasta de fallback é obrigatório."));

        ValidateExtensions(instance.AllowedExtensions, errors);
        ValidateCategories(instance.Categories, errors);

        return errors.Count == 0 ? ValidationResult.Success : ValidationResult.Fail(errors);
    }

    private static void ValidateExtensions(IReadOnlyList<string>? extensions, List<ValidationError> errors)
    {
        if (extensions is null || extensions.Count == 0)
        {
            errors.Add(new ValidationError("allowedExtensions", "Informe ao menos uma extensão permitida."));
            return;
        }

        foreach (var ext in extensions)
        {
            if (string.IsNullOrWhiteSpace(ext))
            {
                errors.Add(new ValidationError("allowedExtensions", "Extensão permitida inválida."));
                continue;
            }

            if (!ext.Trim().StartsWith(".", StringComparison.Ordinal))
                errors.Add(new ValidationError("allowedExtensions", $"Extensão inválida: '{ext}'. Use o formato '.ext'."));
        }
    }

    private static void ValidateCategories(IReadOnlyList<OrganizerCategory>? categories, List<ValidationError> errors)
    {
        if (categories is null || categories.Count == 0)
        {
            errors.Add(new ValidationError("categories", "Informe ao menos uma categoria."));
            return;
        }

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var folders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < categories.Count; i++)
        {
            var category = categories[i];
            var index = i + 1;

            if (string.IsNullOrWhiteSpace(category.Name))
                errors.Add(new ValidationError("categories", $"Categoria {index}: nome obrigatório."));
            else if (!names.Add(category.Name.Trim()))
                errors.Add(new ValidationError("categories", $"Categoria '{category.Name}': nome duplicado."));

            if (string.IsNullOrWhiteSpace(category.Folder))
                errors.Add(new ValidationError("categories", $"Categoria {index}: pasta obrigatória."));
            else if (!folders.Add(category.Folder.Trim()))
                errors.Add(new ValidationError("categories", $"Categoria '{category.Name}': pasta duplicada."));

            if (category.KeywordWeight < 0)
                errors.Add(new ValidationError("categories", $"Categoria '{category.Name}': peso de palavra-chave inválido."));

            if (category.NegativeWeight < 0)
                errors.Add(new ValidationError("categories", $"Categoria '{category.Name}': peso negativo inválido."));

            if (category.MinScore is < 0)
                errors.Add(new ValidationError("categories", $"Categoria '{category.Name}': minScore inválido."));

            if (category.Keywords is null || category.Keywords.Length == 0 || category.Keywords.All(string.IsNullOrWhiteSpace))
                errors.Add(new ValidationError("categories", $"Categoria '{category.Name}': informe ao menos uma palavra-chave."));
        }
    }
}
