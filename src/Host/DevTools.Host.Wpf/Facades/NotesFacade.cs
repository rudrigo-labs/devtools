using DevTools.Core.Results;
using DevTools.Core.Validation;
using DevTools.Notes.Abstractions;
using DevTools.Notes.Engine;
using DevTools.Notes.Models;
using DevTools.Notes.Providers;
using DevTools.Notes.Services;

namespace DevTools.Host.Wpf.Facades;

public interface INotesFacade
{
    Task<IReadOnlyList<NotesEntity>> LoadConfigurationsAsync(CancellationToken ct = default);
    Task<ValidationResult> SaveConfigurationAsync(NotesEntity entity, CancellationToken ct = default);
    Task DeleteConfigurationAsync(string id, CancellationToken ct = default);
    Task<RunResult<NotesResponse>> ExecuteAsync(NotesRequest request, NotesEntity? config = null, CancellationToken ct = default);

    /// <summary>Inicia o fluxo OAuth2 — abre o browser para autorização.</summary>
    Task<RunResult<bool>> ConnectGoogleDriveAsync(NotesEntity config, CancellationToken ct = default);

    /// <summary>Revoga o token OAuth2 salvo.</summary>
    Task<RunResult<bool>> DisconnectGoogleDriveAsync(NotesEntity config, CancellationToken ct = default);
}

public sealed class NotesFacade : INotesFacade
{
    private readonly NotesEntityService _entityService;
    private readonly GoogleDriveAuthService _authService;

    public NotesFacade(NotesEntityService entityService, GoogleDriveAuthService authService)
    {
        _entityService = entityService;
        _authService   = authService;
    }

    public Task<IReadOnlyList<NotesEntity>> LoadConfigurationsAsync(CancellationToken ct = default) =>
        _entityService.ListAsync(ct);

    public Task<ValidationResult> SaveConfigurationAsync(NotesEntity entity, CancellationToken ct = default) =>
        _entityService.UpsertAsync(entity, ct);

    public Task DeleteConfigurationAsync(string id, CancellationToken ct = default) =>
        _entityService.DeleteAsync(id, ct);

    public async Task<RunResult<NotesResponse>> ExecuteAsync(
        NotesRequest request,
        NotesEntity? config = null,
        CancellationToken ct = default)
    {
        IGoogleDriveNotesStore? driveStore = null;

        if (config?.GoogleDriveEnabled == true
            && !string.IsNullOrWhiteSpace(config.GoogleDriveCredentialsPath)
            && !string.IsNullOrWhiteSpace(config.GoogleDriveFolderId)
            && !string.IsNullOrWhiteSpace(config.OAuthTokenCachePath))
        {
            var auth = await _authService.AuthenticateAsync(
                config.GoogleDriveCredentialsPath,
                config.OAuthTokenCachePath,
                ct);

            if (auth.IsSuccess && auth.Value is not null)
                driveStore = new GoogleDriveNotesStore(auth.Value, config.GoogleDriveFolderId);
        }

        var itemsRepository = new DevTools.Notes.Repositories.NotesItemsRepository();
        var backupStore = new DevTools.Notes.Providers.NotesBackupStore();
        var engine = new NotesEngine(itemsRepository, backupStore, driveStore);

        // Aplicar raiz da configuração se não vier no request
        if (config is not null && string.IsNullOrWhiteSpace(request.NotesRootPath))
            request.NotesRootPath = config.LocalRootPath;

        return await engine.ExecuteAsync(request, ct: ct);
    }

    public Task<RunResult<bool>> ConnectGoogleDriveAsync(NotesEntity config, CancellationToken ct = default) =>
        _authService.AuthenticateAsync(
            config.GoogleDriveCredentialsPath,
            config.OAuthTokenCachePath,
            ct)
        .ContinueWith(t => t.Result.IsSuccess
            ? RunResult<bool>.Success(true)
            : RunResult<bool>.Fail(t.Result.Errors), ct);

    public Task<RunResult<bool>> DisconnectGoogleDriveAsync(NotesEntity config, CancellationToken ct = default) =>
        _authService.RevokeTokenAsync(
            config.GoogleDriveCredentialsPath,
            config.OAuthTokenCachePath,
            ct);
}
