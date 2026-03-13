using DevTools.Core.Validation;
using DevTools.Notes.Models;

namespace DevTools.Notes.Validators;

public sealed class NotesEntityValidator : IValidator<NotesEntity>
{
    public ValidationResult Validate(NotesEntity instance)
    {
        if (instance is null)
            return ValidationResult.Fail(new ValidationError("entity", "Configuração não pode ser nula."));

        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(instance.Name))
            errors.Add(new ValidationError("name", "Nome é obrigatório."));

        var ext = instance.DefaultExtension?.Trim().ToLowerInvariant();
        if (ext != ".md" && ext != ".txt")
            errors.Add(new ValidationError("defaultExtension", "Extensão deve ser .md ou .txt."));

        if (instance.GoogleDriveEnabled)
        {
            if (string.IsNullOrWhiteSpace(instance.GoogleDriveCredentialsPath))
                errors.Add(new ValidationError("googleDriveCredentialsPath", "Caminho do credentials.json é obrigatório quando o Google Drive está ativado."));

            if (string.IsNullOrWhiteSpace(instance.GoogleDriveFolderId))
                errors.Add(new ValidationError("googleDriveFolderId", "ID da pasta do Google Drive é obrigatório quando o Drive está ativado."));

            if (string.IsNullOrWhiteSpace(instance.OAuthTokenCachePath))
                errors.Add(new ValidationError("oAuthTokenCachePath", "Pasta do token OAuth2 é obrigatória quando o Google Drive está ativado."));
        }

        return errors.Count == 0 ? ValidationResult.Success : ValidationResult.Fail(errors);
    }
}
