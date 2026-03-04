using System;
using System.Collections.Generic;
using System.Linq;
using DevTools.Core.Models;

namespace DevTools.Core.Configuration;

public class ProfileManager
{
    private readonly IProfileStore _store;

    public ProfileManager()
        : this(new JsonFileProfileStore())
    {
    }

    public ProfileManager(IProfileStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public List<ToolProfile> LoadProfiles(string toolName)
    {
        return _store.LoadProfiles(toolName);
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

    public void SaveProfiles(string toolName, List<ToolProfile> profiles)
    {
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

    public ToolProfile? GetDefaultProfile(string toolName)
    {
        var profiles = LoadProfiles(toolName);
        return profiles.FirstOrDefault(p => p.IsDefault);
    }

    private void SaveProfilesInternal(string toolName, List<ToolProfile> profiles)
    {
        _store.SaveProfiles(toolName, profiles);
    }
}
