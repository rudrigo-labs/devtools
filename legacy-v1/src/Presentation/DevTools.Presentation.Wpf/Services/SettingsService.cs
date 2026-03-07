using System;
using System.IO;
using System.Text.Json;
using DevTools.Presentation.Wpf.Models;

namespace DevTools.Presentation.Wpf.Services;

public class SettingsService
{
    private readonly string _filePath;
    private AppSettings _settings;

    public SettingsService()
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DevTools");
        Directory.CreateDirectory(folder);
        _filePath = Path.Combine(folder, "settings.json");
        _settings = LoadSettings();
    }

    public AppSettings Settings => _settings;

    private AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch
        {
            // Ignore errors, return default
        }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // Ignore errors
        }
    }
}
