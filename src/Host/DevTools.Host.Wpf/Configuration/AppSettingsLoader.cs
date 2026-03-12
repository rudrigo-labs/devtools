using System.IO;
using System.Text.Json;
using DevTools.Core.Models;

namespace DevTools.Host.Wpf.Configuration;

/// <summary>
/// Lê o appsettings.json na pasta do executável e retorna AppSettings populado.
/// Se o arquivo não existir, retorna os defaults definidos no modelo.
/// </summary>
public static class AppSettingsLoader
{
    private const string FileName = "appsettings.json";

    public static AppSettings Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, FileName);

        if (!File.Exists(path))
            return new AppSettings();

        try
        {
            var json = File.ReadAllText(path);
            var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("DevTools", out var devToolsElement))
                return new AppSettings();

            var settings = new AppSettings();

            if (devToolsElement.TryGetProperty("FileTools", out var fileToolsElement))
            {
                if (fileToolsElement.TryGetProperty("MaxFileSizeKb", out var maxEl) &&
                    maxEl.TryGetInt32(out var max) && max > 0)
                    settings.FileTools.MaxFileSizeKb = max;

                if (fileToolsElement.TryGetProperty("AbsoluteMaxFileSizeKb", out var absEl) &&
                    absEl.TryGetInt32(out var abs) && abs > 0)
                    settings.FileTools.AbsoluteMaxFileSizeKb = abs;
            }

            return settings;
        }
        catch
        {
            // Se o arquivo estiver malformado, usa os defaults — não quebra a aplicação.
            return new AppSettings();
        }
    }
}
