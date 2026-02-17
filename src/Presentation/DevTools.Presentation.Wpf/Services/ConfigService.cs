using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using DevTools.SSHTunnel.Models;
using DevTools.Harvest.Configuration;

namespace DevTools.Presentation.Wpf.Services;

public class SshConfigSection
{
    public List<TunnelProfile> Profiles { get; set; } = new();
}

public class ConfigService
{
    private readonly string _configPath;
    
    public ConfigService()
    {
        // Alterado para a raiz da aplicação (onde o .exe está)
        _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
    }

    public string ConfigPath => _configPath;

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
                return JsonSerializer.Deserialize<T>(section.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new T();
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
        var root = new System.Collections.Generic.Dictionary<string, object>();
        
        // Read existing
        if (File.Exists(_configPath))
        {
            try
            {
                var json = File.ReadAllText(_configPath);
                root = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(json) ?? new();
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to read existing config file '{_configPath}' before saving section '{sectionName}'.", ex);
            }
        }

        // Update section
        // We use JsonElement to keep other sections intact, but for the updated section we need to serialize it to object (or JsonElement)
        // A simple way is to serialize 'data' to JsonElement and put it in the dictionary
        var sectionJson = JsonSerializer.SerializeToElement(data);
        root[sectionName] = sectionJson;

        // Save back
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var finalJson = JsonSerializer.Serialize(root, options);
            File.WriteAllText(_configPath, finalJson);
        }
        catch { }
    }

    public void CreateDefaultIfNotExists()
    {
        if (File.Exists(_configPath))
            return;

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
                Profiles = new[] 
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
                        StrictHostKeyChecking = "Default", // Default, Yes, No, Ask
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
                    ExcludeDirectories = new List<string> { "bin", "obj", ".git", "node_modules" }
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
