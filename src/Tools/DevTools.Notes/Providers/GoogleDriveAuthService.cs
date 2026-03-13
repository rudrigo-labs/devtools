using DevTools.Core.Results;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace DevTools.Notes.Providers;

public sealed class GoogleDriveAuthService
{
    private static readonly string[] Scopes = { DriveService.Scope.DriveFile };
    private const string AppName = "DevTools Notes";

    /// <summary>
    /// Cria um DriveService autenticado via OAuth2.
    /// Na primeira vez abre o browser para o usuário autorizar.
    /// Nas próximas vezes usa o token salvo em <paramref name="tokenCachePath"/>.
    /// </summary>
    public async Task<RunResult<DriveService>> AuthenticateAsync(
        string credentialsPath,
        string tokenCachePath,
        CancellationToken ct = default)
    {
        try
        {
            if (!File.Exists(credentialsPath))
                return RunResult<DriveService>.Fail(new ErrorDetail(
                    "notes.drive.credentials.notfound",
                    $"Arquivo credentials.json não encontrado em: {credentialsPath}"));

            using var stream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read);

            Directory.CreateDirectory(tokenCachePath);

            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.FromStream(stream).Secrets,
                Scopes,
                "user",
                ct,
                new FileDataStore(tokenCachePath, fullPath: true));

            var service = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = AppName
            });

            return RunResult<DriveService>.Success(service);
        }
        catch (OperationCanceledException)
        {
            return RunResult<DriveService>.Fail(new ErrorDetail(
                "notes.drive.auth.cancelled",
                "Autenticação cancelada pelo usuário."));
        }
        catch (Exception ex)
        {
            return RunResult<DriveService>.Fail(new ErrorDetail(
                "notes.drive.auth.failed",
                "Falha na autenticação com o Google Drive.",
                Exception: ex));
        }
    }

    /// <summary>Revoga e remove o token salvo (logout).</summary>
    public async Task<RunResult<bool>> RevokeTokenAsync(
        string credentialsPath,
        string tokenCachePath,
        CancellationToken ct = default)
    {
        try
        {
            if (!File.Exists(credentialsPath))
                return RunResult<bool>.Fail(new ErrorDetail(
                    "notes.drive.credentials.notfound",
                    "Arquivo credentials.json não encontrado."));

            using var stream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read);

            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.FromStream(stream).Secrets,
                Scopes,
                "user",
                ct,
                new FileDataStore(tokenCachePath, fullPath: true));

            await credential.RevokeTokenAsync(ct);

            // Limpar arquivos de token locais
            if (Directory.Exists(tokenCachePath))
            {
                foreach (var f in Directory.GetFiles(tokenCachePath))
                    File.Delete(f);
            }

            return RunResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return RunResult<bool>.Fail(new ErrorDetail(
                "notes.drive.revoke.failed",
                "Falha ao revogar token do Google Drive.",
                Exception: ex));
        }
    }
}
