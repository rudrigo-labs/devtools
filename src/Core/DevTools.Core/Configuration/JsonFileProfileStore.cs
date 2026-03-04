using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using DevTools.Core.Models;

namespace DevTools.Core.Configuration;

public sealed class JsonFileProfileStore : IProfileStore
{
    private readonly string _basePath;

    public JsonFileProfileStore()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _basePath = Path.Combine(appData, "DevTools", "profiles");

        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }

    public List<ToolProfile> LoadProfiles(string toolName)
    {
        var path = GetFilePath(toolName);
        if (!File.Exists(path))
        {
            return new List<ToolProfile>();
        }

        try
        {
            var json = File.ReadAllText(path);
            var container = JsonSerializer.Deserialize<ToolProfileContainer>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return container?.Profiles ?? new List<ToolProfile>();
        }
        catch
        {
            return new List<ToolProfile>();
        }
    }

    public void SaveProfiles(string toolName, List<ToolProfile> profiles)
    {
        var path = GetFilePath(toolName);
        var container = new ToolProfileContainer { Profiles = profiles };
        var json = JsonSerializer.Serialize(container, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    private string GetFilePath(string toolName)
    {
        var safeName = string.Join("_", toolName.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(_basePath, $"devtools.{safeName.ToLowerInvariant()}.json");
    }
}

