using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DevTools.Harvest.Configuration;
using DevTools.Presentation.Wpf.Services;

namespace DevTools.Presentation.Wpf.Persistence.Stores;

public sealed class JsonSettingsStore : ISettingsStore
{
    private readonly string _configPath;

    public JsonSettingsStore(string configPath)
    {
        _configPath = configPath;
    }

    public string Location => _configPath;

    public bool IsConfigured()
    {
        if (!File.Exists(_configPath))
        {
            AppLogger.Error($"Config file not found at '{_configPath}'.");
            return false;
        }

        try
        {
            var content = File.ReadAllText(_configPath);
            if (string.IsNullOrWhiteSpace(content))
            {
                AppLogger.Error($"Config file '{_configPath}' is empty.");
                return false;
            }

            JsonDocument.Parse(content);
            return true;
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Failed to read or parse config file '{_configPath}'.", ex);
            return false;
        }
    }

    public T GetSection<T>(string sectionName) where T : new()
    {
        if (!File.Exists(_configPath))
        {
            AppLogger.Error($"Config file not found when reading section '{sectionName}'. Path: '{_configPath}'.");
            return new T();
        }

        try
        {
            var json = File.ReadAllText(_configPath);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty(sectionName, out var section))
            {
                return JsonSerializer.Deserialize<T>(
                           section.GetRawText(),
                           new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                       ?? new T();
            }

            AppLogger.Error($"Config section '{sectionName}' not found in '{_configPath}'.");
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Failed to read config section '{sectionName}' from '{_configPath}'.", ex);
        }

        return new T();
    }

    public void SaveSection<T>(string sectionName, T data)
    {
        var root = new Dictionary<string, object>();

        if (File.Exists(_configPath))
        {
            try
            {
                var json = File.ReadAllText(_configPath);
                root = JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new();
            }
            catch (Exception ex)
            {
                AppLogger.Error(
                    $"Failed to read existing config file '{_configPath}' before saving section '{sectionName}'.",
                    ex);
            }
        }

        var sectionJson = JsonSerializer.SerializeToElement(data);
        root[sectionName] = sectionJson;

        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var finalJson = JsonSerializer.Serialize(root, options);
            File.WriteAllText(_configPath, finalJson);
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Failed to save config section '{sectionName}' to '{_configPath}'.", ex);
        }
    }

    public void CreateDefaultIfNotExists()
    {
        if (File.Exists(_configPath))
        {
            return;
        }

        var defaultContent = new
        {
            ConnectionStrings = new { },
            Email = new
            {
                SmtpHost = "",
                SmtpPort = 587,
                Username = "",
                Password = ""
            },
            Ssh = new
            {
                Configurations = new[]
                {
                    new
                    {
                        Name = "default",
                        SshHost = "",
                        SshPort = 22,
                        SshUser = "",
                        LocalBindHost = "127.0.0.1",
                        LocalPort = 14331,
                        RemoteHost = "127.0.0.1",
                        RemotePort = 1433,
                        StrictHostKeyChecking = "Default",
                        ConnectTimeoutSeconds = (int?)null
                    }
                }
            },
            Harvest = new HarvestConfig
            {
                MinScoreDefault = 0,
                TopDefault = 100,
                Weights = new HarvestWeights
                {
                    FanInWeight = 2.0,
                    FanOutWeight = 0.5,
                    KeywordDensityWeight = 1.0,
                    DeadCodePenalty = 5.0
                },
                Rules = new HarvestRules
                {
                    Extensions = new List<string> { ".cs", ".xml", ".json", ".xaml" },
                    ExcludeDirectories = HarvestDefaults.DefaultExcludeDirectories.ToList()
                }
            }
        };

        try
        {
            var json = JsonSerializer.Serialize(defaultContent, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_configPath, json);
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Failed to create default config file at '{_configPath}'.", ex);
        }
    }
}

