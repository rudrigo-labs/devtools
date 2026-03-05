using DevTools.Ngrok.Models;
using DevTools.Ngrok.Services;

namespace DevTools.Ngrok.Engine;

public sealed class NgrokConfigEngine
{
    private readonly INgrokSettingsStore _settingsStore;

    public NgrokConfigEngine(INgrokSettingsStore? settingsStore = null)
    {
        _settingsStore = settingsStore ?? NgrokSettingsStoreFactory.CreateDefault();
    }

    public NgrokSettings GetSettings()
    {
        var settings = _settingsStore.Load() ?? new NgrokSettings();
        settings.Normalize();
        return settings;
    }

    public bool IsConfigured()
    {
        var settings = GetSettings();
        return !string.IsNullOrWhiteSpace(settings.AuthToken);
    }

    public void SaveAuthtoken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Authtoken is required.", nameof(token));

        var settings = GetSettings();
        settings.AuthToken = token.Trim();
        _settingsStore.Save(settings);
    }

    public void SaveSettings(NgrokSettings settings)
    {
        if (settings is null)
            throw new ArgumentNullException(nameof(settings));

        settings.Normalize();
        _settingsStore.Save(settings);
    }
}
