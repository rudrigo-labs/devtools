using DevTools.Ngrok.Models;

namespace DevTools.Ngrok.Services;

public interface INgrokSettingsStore
{
    NgrokSettings Load();
    void Save(NgrokSettings settings);
}
