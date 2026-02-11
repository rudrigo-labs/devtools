using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Identity.Client;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;

namespace DevTools.Notes.Cloud;

public class OneDriveProvider : ICloudProvider
{
    private readonly ITokenStore _tokenStore;
    private readonly string _clientId;
    private IPublicClientApplication? _pca;
    private GraphServiceClient? _graphClient;
    private string? _driveId;
    private string[] _scopes = new[] { "Files.ReadWrite.AppFolder", "User.Read" };
    private IAccount? _currentAccount;

    public CloudProviderType ProviderType => CloudProviderType.OneDrive;
    public bool IsConnected => _currentAccount != null;
    public string? UserDisplayName => _currentAccount?.Username;

    public OneDriveProvider(ITokenStore tokenStore, string clientId)
    {
        _tokenStore = tokenStore;
        _clientId = clientId;
    }

    private void EnsurePca()
    {
        if (_pca == null)
        {
            _pca = PublicClientApplicationBuilder.Create(_clientId)
                .WithRedirectUri("http://localhost") // Recommended for desktop apps
                .Build();

            var cacheHelper = new MsalTokenCacheHelper(_tokenStore, "onedrive_cache");
            cacheHelper.EnableSerialization(_pca.UserTokenCache);
        }
    }

    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        EnsurePca();
        if (_pca == null) return false;

        try
        {
            // Try silent first
            var accounts = await _pca.GetAccountsAsync();
            _currentAccount = accounts.FirstOrDefault();

            if (_currentAccount == null)
            {
                // Interactive
                var result = await _pca.AcquireTokenInteractive(_scopes)
                    .WithPrompt(Microsoft.Identity.Client.Prompt.SelectAccount)
                    .ExecuteAsync(cancellationToken);
                
                _currentAccount = result.Account;
            }
            else
            {
                // Refresh token check
                await _pca.AcquireTokenSilent(_scopes, _currentAccount).ExecuteAsync(cancellationToken);
            }

            InitializeGraphClient();
            
            // Get Drive ID
            if (_graphClient != null)
            {
                var drive = await _graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);
                _driveId = drive?.Id;
            }

            return true;
        }
        catch (Exception)
        {
            _currentAccount = null;
            return false;
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        EnsurePca();
        if (_pca != null)
        {
            var accounts = await _pca.GetAccountsAsync();
            foreach (var account in accounts)
            {
                await _pca.RemoveAsync(account);
            }
        }
        _currentAccount = null;
        _graphClient = null;
    }

    private void InitializeGraphClient()
    {
        if (_pca == null) return;

        // Custom authentication provider that uses MSAL
        var authProvider = new BaseBearerTokenAuthenticationProvider(new MsalAuthenticationProvider(_pca, _scopes));
        _graphClient = new GraphServiceClient(authProvider);
    }

    public async Task<List<CloudFileMetadata>> ListFilesAsync(CancellationToken cancellationToken = default)
    {
        if (_graphClient == null || _driveId == null) throw new InvalidOperationException("Not connected");

        // List files in App Root (special/approot)
        // Workaround: Use AppRoot ID and manual children listing if builder is missing properties
        var appRoot = await _graphClient.Drives[_driveId].Special["approot"].GetAsync(cancellationToken: cancellationToken);
        if (appRoot?.Id == null) return new List<CloudFileMetadata>();

        // We use the generic RequestAdapter to bypass missing SDK properties if necessary
        // But let's try to see if we can get children by iterating or using a raw request
        // Since Children property is missing in this SDK version for DriveItemItemRequestBuilder (allegedly),
        // we construct the request manually.

        var requestInfo = _graphClient.Drives[_driveId].Items[appRoot.Id].ToGetRequestInformation();
        if (requestInfo?.UrlTemplate == null) return new List<CloudFileMetadata>();

        requestInfo.UrlTemplate = requestInfo.UrlTemplate.Replace("{driveItem%2Did}", appRoot.Id) + "/children";
        requestInfo.QueryParameters.Add("select", new[] { "id", "name", "lastModifiedDateTime", "size", "file" });

        var result = await _graphClient.RequestAdapter.SendAsync(
            requestInfo,
            DriveItemCollectionResponse.CreateFromDiscriminatorValue,
            null,
            cancellationToken);

        var list = new List<CloudFileMetadata>();
        if (result?.Value != null)
        {
            foreach (var item in result.Value)
            {
                if (item.File != null) // Only files
                {
                    list.Add(new CloudFileMetadata
                    {
                        Id = item.Id ?? "",
                        Name = item.Name ?? "",
                        ModifiedTime = item.LastModifiedDateTime?.DateTime,
                        Size = item.Size,
                        ContentHash = item.File.Hashes?.Sha1Hash 
                    });
                }
            }
        }
        return list;
    }

    public async Task<string> UploadFileAsync(string localFilePath, string remoteFileName, CancellationToken cancellationToken = default)
    {
        if (_graphClient == null || _driveId == null) throw new InvalidOperationException("Not connected");

        using var stream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read);
        
        // 1. Get AppRoot ID
        var appRoot = await _graphClient.Drives[_driveId].Special["approot"].GetAsync(cancellationToken: cancellationToken);
        if (appRoot?.Id == null) throw new InvalidOperationException("AppRoot not found");

        // 2. Try to address file by path relative to AppRoot? 
        // Graph API supports special/approot:/filename
        // But SDK ItemWithPath on DriveItemItemRequestBuilder is missing.
        // We will list children to find if it exists, to get ID, OR just use the /content endpoint on the child if we can address it.
        // Let's try to construct the URL for the child manually: /drives/{id}/items/{rootId}:/{filename}:/content
        
        // Manual PUT request
        var requestInfo = _graphClient.Drives[_driveId].Items[appRoot.Id].ToGetRequestInformation();
        // Construct URL: .../items/{appRootId}:/{remoteFileName}:/content
        if (requestInfo?.UrlTemplate != null)
        {
             requestInfo.UrlTemplate = requestInfo.UrlTemplate.Replace("{driveItem%2Did}", appRoot.Id) + $":/{remoteFileName}:/content";
        }
        
        if (requestInfo != null)
        {
            requestInfo.HttpMethod = Method.PUT;
            requestInfo.SetStreamContent(stream, "application/octet-stream");

            // We need to execute this. But RequestAdapter.SendAsync returns a Model.
            // PUT /content returns DriveItem.
            
            var uploadedItem = await _graphClient.RequestAdapter.SendAsync(
                requestInfo,
                DriveItem.CreateFromDiscriminatorValue,
                null,
                cancellationToken);

            return uploadedItem?.Id ?? "";
        }
        return "";
    }

    public async Task DownloadFileAsync(string remoteFileId, string localFilePath, CancellationToken cancellationToken = default)
    {
        if (_graphClient == null || _driveId == null) throw new InvalidOperationException("Not connected");

        var stream = await _graphClient.Drives[_driveId].Items[remoteFileId].Content.GetAsync(null, cancellationToken);
        
        if (stream != null)
        {
            using var fileStream = new FileStream(localFilePath, FileMode.Create, FileAccess.Write);
            await stream.CopyToAsync(fileStream, cancellationToken);
        }
    }

    public async Task DeleteFileAsync(string remoteFileId, CancellationToken cancellationToken = default)
    {
        if (_graphClient == null || _driveId == null) throw new InvalidOperationException("Not connected");

        await _graphClient.Drives[_driveId].Items[remoteFileId].DeleteAsync(null, cancellationToken);
    }
}

// Simple adapter to bridge MSAL and Graph SDK
public class MsalAuthenticationProvider : IAccessTokenProvider
{
    private readonly IPublicClientApplication _pca;
    private readonly string[] _scopes;

    public MsalAuthenticationProvider(IPublicClientApplication pca, string[] scopes)
    {
        _pca = pca;
        _scopes = scopes;
    }

    public async Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
    {
        var accounts = await _pca.GetAccountsAsync();
        var account = accounts.FirstOrDefault();
        
        if (account == null) throw new InvalidOperationException("No account to acquire token silently.");

        var result = await _pca.AcquireTokenSilent(_scopes, account).ExecuteAsync(cancellationToken);
        return result.AccessToken;
    }

    public AllowedHostsValidator AllowedHostsValidator => new AllowedHostsValidator();
}
