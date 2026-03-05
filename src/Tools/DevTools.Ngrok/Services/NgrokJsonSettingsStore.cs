using System.Text.Json;
using DevTools.Ngrok.Models;

namespace DevTools.Ngrok.Services;

public sealed class NgrokJsonSettingsStore : INgrokSettingsStore
{
    private readonly string _settingsFilePath;

    public NgrokJsonSettingsStore()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DevTools",
            "ngrok.settings.json"))
    {
    }

    public NgrokJsonSettingsStore(string settingsFilePath)
    {
        _settingsFilePath = settingsFilePath;
    }

    public NgrokSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsFilePath))
                return new NgrokSettings();

            var json = File.ReadAllText(_settingsFilePath);
            var settings = JsonSerializer.Deserialize<NgrokSettings>(json) ?? new NgrokSettings();
            settings.Normalize();
            return settings;
        }
        catch
        {
            return new NgrokSettings();
        }
    }

    public void Save(NgrokSettings settings)
    {
        if (settings is null)
            throw new ArgumentNullException(nameof(settings));

        settings.Normalize();
        EnsureDirectory();
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_settingsFilePath, json);
    }

    private void EnsureDirectory()
    {
        var dir = Path.GetDirectoryName(_settingsFilePath);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);
    }
}
