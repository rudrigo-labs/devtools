using DevTools.Core.Models;

namespace DevTools.Notes.Models;

public sealed class NotesEntity : NamedConfiguration
{
    /// <summary>Pasta raiz onde as notas são salvas localmente.</summary>
    public string LocalRootPath { get; set; } = string.Empty;

    /// <summary>Extensão padrão para novas notas: ".md" ou ".txt".</summary>
    public string DefaultExtension { get; set; } = ".md";

    // ── Google Drive ──────────────────────────────────────────────────────────

    public bool GoogleDriveEnabled { get; set; }

    /// <summary>Caminho para o arquivo credentials.json baixado do Google Cloud Console.</summary>
    public string GoogleDriveCredentialsPath { get; set; } = string.Empty;

    /// <summary>ID da pasta no Google Drive onde as notas serão salvas.</summary>
    public string GoogleDriveFolderId { get; set; } = string.Empty;

    /// <summary>Pasta local onde o token OAuth2 será armazenado.</summary>
    public string OAuthTokenCachePath { get; set; } = string.Empty;
}
