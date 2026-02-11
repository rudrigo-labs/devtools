using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DevTools.Notes.Cloud;

public interface ICloudProvider
{
    CloudProviderType ProviderType { get; }
    bool IsConnected { get; }
    string? UserDisplayName { get; }

    Task<bool> ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    
    // Core operations
    Task<List<CloudFileMetadata>> ListFilesAsync(CancellationToken cancellationToken = default);
    Task<string> UploadFileAsync(string localFilePath, string remoteFileName, CancellationToken cancellationToken = default);
    Task DownloadFileAsync(string remoteFileId, string localFilePath, CancellationToken cancellationToken = default);
    Task DeleteFileAsync(string remoteFileId, CancellationToken cancellationToken = default);
}
