using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DevTools.Notes.Cloud;

public class SyncResult
{
    public int Uploaded { get; set; }
    public int Downloaded { get; set; }
    public int Conflicts { get; set; }
    public int Errors { get; set; }
    public List<string> Messages { get; set; } = new List<string>();
}

public class SyncEngine
{
    private readonly ICloudProvider _provider;
    private readonly string _localDirectory;
    private const string Separator = "__";

    public SyncEngine(ICloudProvider provider, string localDirectory)
    {
        _provider = provider;
        _localDirectory = localDirectory;
    }

    public async Task<SyncResult> SyncAsync(CancellationToken cancellationToken = default)
    {
        var result = new SyncResult();

        if (!_provider.IsConnected)
        {
            result.Messages.Add("Provider not connected.");
            return result;
        }

        try
        {
            // 1. Get Local Files (Recursive, Flat mapped)
            // Map: RemoteName -> LocalFileInfo
            if (!Directory.Exists(_localDirectory))
                Directory.CreateDirectory(_localDirectory);

            var localFiles = Directory.GetFiles(_localDirectory, "*", SearchOption.AllDirectories)
                .Select(f => new FileInfo(f))
                .Where(f => !f.Name.Equals("thumbs.db", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(f => ToRemoteName(f.FullName));

            // 2. Get Remote Files
            var remoteFiles = await _provider.ListFilesAsync(cancellationToken);
            var remoteFilesMap = remoteFiles.ToDictionary(f => f.Name);

            // 3. Process Local Files (Upload or Conflict Check)
            foreach (var kvp in localFiles)
            {
                var remoteName = kvp.Key;
                var localFile = kvp.Value;

                if (remoteFilesMap.TryGetValue(remoteName, out var remoteFile))
                {
                    // Exists in both. Compare.
                    var timeDiff = localFile.LastWriteTimeUtc - (remoteFile.ModifiedTime ?? DateTime.MinValue);
                    
                    if (timeDiff.TotalSeconds > 5)
                    {
                        // Local is newer -> Upload
                        await _provider.UploadFileAsync(localFile.FullName, remoteName, cancellationToken);
                        result.Uploaded++;
                        result.Messages.Add($"Uploaded updated: {localFile.Name}");
                    }
                    else if (timeDiff.TotalSeconds < -5)
                    {
                        // Remote is newer -> Conflict
                        // Download to conflict file
                        var conflictPath = GetConflictPath(localFile.FullName);
                        await _provider.DownloadFileAsync(remoteFile.Id, conflictPath, cancellationToken);
                        result.Conflicts++;
                        result.Messages.Add($"Conflict: {localFile.Name}");
                    }
                }
                else
                {
                    // Local only -> Upload
                    await _provider.UploadFileAsync(localFile.FullName, remoteName, cancellationToken);
                    result.Uploaded++;
                    result.Messages.Add($"Uploaded new: {localFile.Name}");
                }
            }

            // 4. Process Remote Files (Download new ones)
            foreach (var remoteFile in remoteFiles)
            {
                if (!localFiles.ContainsKey(remoteFile.Name))
                {
                    // Remote only -> Download
                    var localPath = ToLocalPath(remoteFile.Name);
                    
                    // Ensure directory exists
                    var dir = Path.GetDirectoryName(localPath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
                    
                    await _provider.DownloadFileAsync(remoteFile.Id, localPath, cancellationToken);
                    result.Downloaded++;
                    result.Messages.Add($"Downloaded new: {remoteFile.Name}");
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors++;
            result.Messages.Add($"Sync failed: {ex.Message}");
        }

        return result;
    }

    private string ToRemoteName(string localPath)
    {
        var rel = Path.GetRelativePath(_localDirectory, localPath);
        return rel.Replace(Path.DirectorySeparatorChar.ToString(), Separator)
                  .Replace(Path.AltDirectorySeparatorChar.ToString(), Separator);
    }

    private string ToLocalPath(string remoteName)
    {
        var rel = remoteName.Replace(Separator, Path.DirectorySeparatorChar.ToString());
        return Path.Combine(_localDirectory, rel);
    }

    private string GetConflictPath(string localPath)
    {
        var dir = Path.GetDirectoryName(localPath);
        var name = Path.GetFileNameWithoutExtension(localPath);
        var ext = Path.GetExtension(localPath);
        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        return Path.Combine(dir!, $"{name} (conflict remote {timestamp}){ext}");
    }
}
