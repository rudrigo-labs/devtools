using DevTools.Ngrok.Models;
using DevTools.Ngrok.Services;

namespace DevTools.Ngrok.Engine;

public sealed class NgrokConfigEngine
{
    private readonly INgrokSettingsStore _settingsStore;
    private readonly NgrokEnvironmentService _environmentService;

    public NgrokConfigEngine(
        INgrokSettingsStore? settingsStore = null,
        NgrokEnvironmentService? environmentService = null)
    {
        _settingsStore = settingsStore ?? NgrokSettingsStoreFactory.CreateDefault();
        _environmentService = environmentService ?? new NgrokEnvironmentService();
    }

    public NgrokSettings GetSettings()
    {
        var settings = _settingsStore.Load() ?? new NgrokSettings();
        settings.Normalize();

        var environment = _environmentService.Detect(settings.ExecutablePath);
        if (string.IsNullOrWhiteSpace(settings.ExecutablePath) && !string.IsNullOrWhiteSpace(environment.ExecutablePath))
        {
            settings.ExecutablePath = environment.ExecutablePath;
        }

        if (string.IsNullOrWhiteSpace(settings.AuthToken) && !string.IsNullOrWhiteSpace(environment.Authtoken))
        {
            settings.AuthToken = environment.Authtoken;
        }

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

    public NgrokEnvironmentInfo DetectEnvironment(string? configuredExecutablePath = null)
    {
        return _environmentService.Detect(configuredExecutablePath);
    }
}
