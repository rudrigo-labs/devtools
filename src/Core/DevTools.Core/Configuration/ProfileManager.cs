using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DevTools.Core.Models;

namespace DevTools.Core.Configuration;

public class ProfileManager
{
    private readonly string _basePath;

    public ProfileManager()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _basePath = Path.Combine(appData, "DevTools", "profiles");
        
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }

    private string GetFilePath(string toolName)
    {
        // Sanitize tool name just in case
        var safeName = string.Join("_", toolName.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(_basePath, $"devtools.{safeName.ToLowerInvariant()}.json");
    }

    public List<ToolProfile> LoadProfiles(string toolName)
    {
        var path = GetFilePath(toolName);
        if (!File.Exists(path))
            return new List<ToolProfile>();

        try
        {
            var json = File.ReadAllText(path);
            var container = JsonSerializer.Deserialize<ToolProfileContainer>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return container?.Profiles ?? new List<ToolProfile>();
        }
        catch
        {
            // Fail safe: return empty if file is corrupted
            return new List<ToolProfile>();
        }
    }

    public void SaveProfile(string toolName, ToolProfile profile)
    {
        var profiles = LoadProfiles(toolName);
        var existing = profiles.FirstOrDefault(p => p.Name.Equals(profile.Name, StringComparison.OrdinalIgnoreCase));
        
        if (existing != null)
        {
            profiles.Remove(existing);
        }
        
        // Update timestamp
        profile.UpdatedUtc = DateTime.UtcNow;
        profiles.Add(profile);
        
        SaveProfilesInternal(toolName, profiles);
    }
    
    public void DeleteProfile(string toolName, string profileName)
    {
        var profiles = LoadProfiles(toolName);
        var existing = profiles.FirstOrDefault(p => p.Name.Equals(profileName, StringComparison.OrdinalIgnoreCase));
        
        if (existing != null)
        {
            profiles.Remove(existing);
            SaveProfilesInternal(toolName, profiles);
        }
    }
    
    public ToolProfile? GetProfile(string toolName, string profileName)
    {
        var profiles = LoadProfiles(toolName);
        return profiles.FirstOrDefault(p => p.Name.Equals(profileName, StringComparison.OrdinalIgnoreCase));
    }

    private void SaveProfilesInternal(string toolName, List<ToolProfile> profiles)
    {
        var path = GetFilePath(toolName);
        var container = new ToolProfileContainer { Profiles = profiles };
        var json = JsonSerializer.Serialize(container, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }
}
