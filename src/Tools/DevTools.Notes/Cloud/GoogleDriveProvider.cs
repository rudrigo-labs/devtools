using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace DevTools.Notes.Cloud;

public class GoogleDriveProvider : ICloudProvider
{
    private readonly ITokenStore _tokenStore;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _appName;
    
    private UserCredential? _credential;
    private DriveService? _service;
    
    private const string FolderName = "DevTools_Notes";
    private const string FolderMimeType = "application/vnd.google-apps.folder";

    public CloudProviderType ProviderType => CloudProviderType.GoogleDrive;
    public bool IsConnected => _credential != null && _service != null;
    public string? UserDisplayName => "Google User"; // Google API doesn't give simple display name easily without extra scope/call

    public GoogleDriveProvider(ITokenStore tokenStore, string clientId, string clientSecret, string appName = "DevTools.Notes")
    {
        _tokenStore = tokenStore;
        _clientId = clientId;
        _clientSecret = clientSecret;
        _appName = appName;
    }

    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var secrets = new ClientSecrets
            {
                ClientId = _clientId,
                ClientSecret = _clientSecret
            };

            var scopes = new[] { DriveService.Scope.DriveFile }; // Only access to files created by this app or explicit

            _credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                secrets,
                scopes,
                "user",
                cancellationToken,
                new GoogleTokenStoreAdapter(_tokenStore)
            );

            _service = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = _credential,
                ApplicationName = _appName
            });

            return true;
        }
        catch (Exception)
        {
            // Log error?
            return false;
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (_credential != null)
        {
            // Revoke token? Or just delete local?
            // For now, simple local signout
            await _credential.RevokeTokenAsync(cancellationToken);
            _credential = null;
            _service = null;
        }
    }

    public async Task<List<CloudFileMetadata>> ListFilesAsync(CancellationToken cancellationToken = default)
    {
        if (_service == null) throw new InvalidOperationException("Not connected");

        // 1. Find or Create Folder
        var folderId = await GetOrCreateFolderAsync(cancellationToken);
        if (string.IsNullOrEmpty(folderId)) return new List<CloudFileMetadata>();

        // 2. List files in folder
        var request = _service.Files.List();
        request.Q = $"'{folderId}' in parents and trashed = false";
        request.Fields = "files(id, name, modifiedTime, size, md5Checksum)";
        
        var result = await request.ExecuteAsync(cancellationToken);
        
        return result.Files.Select(f => new CloudFileMetadata
        {
            Id = f.Id,
            Name = f.Name,
            ModifiedTime = f.ModifiedTimeDateTimeOffset?.DateTime,
            Size = f.Size,
            ContentHash = f.Md5Checksum // Google Drive provides MD5
        }).ToList();
    }

    public async Task<string> UploadFileAsync(string localFilePath, string remoteFileName, CancellationToken cancellationToken = default)
    {
        if (_service == null) throw new InvalidOperationException("Not connected");

        var folderId = await GetOrCreateFolderAsync(cancellationToken);
        
        // Check if file exists to update or create new
        var existingFileId = await FindFileIdByNameAsync(folderId, remoteFileName, cancellationToken);

        using var stream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read);
        
        if (existingFileId != null)
        {
            // Update
            var fileMetadata = new Google.Apis.Drive.v3.Data.File(); 
            // We don't change name on update usually, but we could
            
            var request = _service.Files.Update(fileMetadata, existingFileId, stream, "application/octet-stream");
            var result = await request.UploadAsync(cancellationToken);
            if (result.Status == Google.Apis.Upload.UploadStatus.Failed)
                throw result.Exception;
                
            return existingFileId;
        }
        else
        {
            // Create
            var fileMetadata = new Google.Apis.Drive.v3.Data.File
            {
                Name = remoteFileName,
                Parents = new List<string> { folderId }
            };
            
            var request = _service.Files.Create(fileMetadata, stream, "application/octet-stream");
            var result = await request.UploadAsync(cancellationToken);
            if (result.Status == Google.Apis.Upload.UploadStatus.Failed)
                throw result.Exception;
                
            return request.ResponseBody.Id;
        }
    }

    public async Task DownloadFileAsync(string remoteFileId, string localFilePath, CancellationToken cancellationToken = default)
    {
        if (_service == null) throw new InvalidOperationException("Not connected");

        var request = _service.Files.Get(remoteFileId);
        using var stream = new FileStream(localFilePath, FileMode.Create, FileAccess.Write);
        await request.DownloadAsync(stream, cancellationToken);
    }

    public async Task DeleteFileAsync(string remoteFileId, CancellationToken cancellationToken = default)
    {
        if (_service == null) throw new InvalidOperationException("Not connected");

        await _service.Files.Delete(remoteFileId).ExecuteAsync(cancellationToken);
    }

    private async Task<string> GetOrCreateFolderAsync(CancellationToken cancellationToken)
    {
        if (_service == null) return string.Empty;

        // Check if folder exists
        var request = _service.Files.List();
        request.Q = $"mimeType = '{FolderMimeType}' and name = '{FolderName}' and trashed = false";
        request.Fields = "files(id)";
        
        var result = await request.ExecuteAsync(cancellationToken);
        var folder = result.Files.FirstOrDefault();

        if (folder != null)
        {
            return folder.Id;
        }

        // Create folder
        var fileMetadata = new Google.Apis.Drive.v3.Data.File
        {
            Name = FolderName,
            MimeType = FolderMimeType
        };
        
        var createRequest = _service.Files.Create(fileMetadata);
        createRequest.Fields = "id";
        var file = await createRequest.ExecuteAsync(cancellationToken);
        
        return file.Id;
    }

    private async Task<string?> FindFileIdByNameAsync(string folderId, string fileName, CancellationToken cancellationToken)
    {
        if (_service == null) return null;

        var request = _service.Files.List();
        request.Q = $"'{folderId}' in parents and name = '{fileName}' and trashed = false";
        request.Fields = "files(id)";
        
        var result = await request.ExecuteAsync(cancellationToken);
        return result.Files.FirstOrDefault()?.Id;
    }
}
