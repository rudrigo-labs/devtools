using System;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using System.Runtime.Versioning;

namespace DevTools.Notes.Cloud;

[SupportedOSPlatform("windows")]
public class SecureTokenStore : ITokenStore
{
    private readonly string _storagePath;
    
    public SecureTokenStore(string appName)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _storagePath = Path.Combine(appData, appName, "Tokens");
        
        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
        }
    }

    public void SaveToken(string key, string token)
    {
        var filePath = GetFilePath(key);
        var tokenBytes = Encoding.UTF8.GetBytes(token);
        
        // Encrypt using DPAPI (CurrentUser scope)
        // This ensures only the user who encrypted it can decrypt it on this machine
        var encryptedBytes = ProtectedData.Protect(tokenBytes, null, DataProtectionScope.CurrentUser);
        
        File.WriteAllBytes(filePath, encryptedBytes);
    }

    public string? GetToken(string key)
    {
        var filePath = GetFilePath(key);
        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var encryptedBytes = File.ReadAllBytes(filePath);
            
            // Decrypt using DPAPI
            var tokenBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
            
            return Encoding.UTF8.GetString(tokenBytes);
        }
        catch
        {
            // If decryption fails (e.g. different user, machine reset), treat as no token
            return null;
        }
    }

    public void DeleteToken(string key)
    {
        var filePath = GetFilePath(key);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    private string GetFilePath(string key)
    {
        // Sanitize key for filename
        var safeKey = string.Join("_", key.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(_storagePath, $"{safeKey}.dat");
    }
}
