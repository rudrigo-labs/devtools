using Microsoft.CodeAnalysis.CSharp;
using DevTools.Core.Models;
using DevTools.Core.Validation;
using DevTools.Rename.Models;

namespace DevTools.Rename.Validators;

public sealed class RenameRequestValidator : IValidator<RenameRequest>
{
    private readonly AppSettings _settings;

    public RenameRequestValidator(AppSettings? settings = null)
    {
        _settings = settings ?? new AppSettings();
    }

    public ValidationResult Validate(RenameRequest request)
    {
        if (request is null)
            return ValidationResult.Fail(new ValidationError("request", "Request nao pode ser nulo."));

        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(request.RootPath))
            errors.Add(new ValidationError("rootPath", "Caminho raiz e obrigatorio."));
        else if (!Directory.Exists(request.RootPath))
            errors.Add(new ValidationError("rootPath", "Caminho raiz nao encontrado."));

        if (string.IsNullOrWhiteSpace(request.OldText))
            errors.Add(new ValidationError("oldText", "Texto original e obrigatorio."));

        if (string.IsNullOrWhiteSpace(request.NewText))
            errors.Add(new ValidationError("newText", "Texto novo e obrigatorio."));

        if (!string.IsNullOrWhiteSpace(request.OldText) &&
            !string.IsNullOrWhiteSpace(request.NewText) &&
            string.Equals(request.OldText, request.NewText, StringComparison.Ordinal))
            errors.Add(new ValidationError("text", "Texto original e novo sao iguais."));

        if (request.MaxDiffLinesPerFile <= 0)
            errors.Add(new ValidationError("maxDiffLinesPerFile", "MaxDiffLinesPerFile deve ser maior que zero."));

        if (!Enum.IsDefined(typeof(RenameMode), request.Mode))
            errors.Add(new ValidationError("mode", "Mode invalido."));

        if (request.MaxFileSizeKb.HasValue)
        {
            if (request.MaxFileSizeKb <= 0)
                errors.Add(new ValidationError("maxFileSizeKb", "MaxFileSizeKb deve ser maior que zero."));
            else if (request.MaxFileSizeKb > _settings.FileTools.AbsoluteMaxFileSizeKb)
                errors.Add(new ValidationError("maxFileSizeKb",
                    $"MaxFileSizeKb nao pode exceder {_settings.FileTools.AbsoluteMaxFileSizeKb} KB."));
        }

        if (!string.IsNullOrWhiteSpace(request.OldText) && !string.IsNullOrWhiteSpace(request.NewText))
        {
            if (request.Mode == RenameMode.NamespaceOnly || request.OldText.Contains('.'))
            {
                if (!IsValidNamespace(request.OldText))
                    errors.Add(new ValidationError("oldText", "OldText deve ser um namespace valido."));
                if (!IsValidNamespace(request.NewText))
                    errors.Add(new ValidationError("newText", "NewText deve ser um namespace valido."));
            }
            else
            {
                if (!SyntaxFacts.IsValidIdentifier(request.OldText))
                    errors.Add(new ValidationError("oldText", "OldText deve ser um identificador valido."));
                if (!SyntaxFacts.IsValidIdentifier(request.NewText))
                    errors.Add(new ValidationError("newText", "NewText deve ser um identificador valido."));
            }
        }

        return errors.Count == 0 ? ValidationResult.Success : ValidationResult.Fail(errors);
    }

    private static bool IsValidNamespace(string value)
    {
        var parts = value.Split('.', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 && parts.All(SyntaxFacts.IsValidIdentifier);
    }
}
