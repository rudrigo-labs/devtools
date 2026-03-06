using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using DevTools.Core.Models;

namespace DevTools.Core.Configuration;

public sealed class JsonFileToolConfigurationStore : IToolConfigurationStore
{
    private readonly string _basePath;

    public JsonFileToolConfigurationStore()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _basePath = Path.Combine(appData, "DevTools", "configurations");

        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }

    public List<ToolConfiguration> LoadConfigurations(string toolName)
    {
        var path = GetFilePath(toolName);
        if (!File.Exists(path))
        {
            return new List<ToolConfiguration>();
        }

        try
        {
            var json = File.ReadAllText(path);
            var container = JsonSerializer.Deserialize<ToolConfigurationContainer>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return container?.Configurations ?? new List<ToolConfiguration>();
        }
        catch
        {
            return new List<ToolConfiguration>();
        }
    }

    public void SaveConfigurations(string toolName, List<ToolConfiguration> configurations)
    {
        var path = GetFilePath(toolName);
        var container = new ToolConfigurationContainer { Configurations = configurations };
        var json = JsonSerializer.Serialize(container, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    private string GetFilePath(string toolName)
    {
        var safeName = string.Join("_", toolName.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(_basePath, $"devtools.{safeName.ToLowerInvariant()}.json");
    }
}



