using System.Text.Json;
using DevTools.Organizer.Models;

namespace DevTools.Organizer.Engine;

internal static class OrganizerConfigLoader
{
    public static OrganizerConfig Load(string? configPath, string outputPath)
    {
        var path = ResolvePath(configPath, outputPath);
        if (path is null)
            return new OrganizerConfig();

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<OrganizerConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new OrganizerConfig();
        }
        catch
        {
            return new OrganizerConfig();
        }
    }

    private static string? ResolvePath(string? configPath, string outputPath)
    {
        if (!string.IsNullOrWhiteSpace(configPath))
            return configPath;

        var candidate = Path.Combine(outputPath, "devtools.docs.json");
        return File.Exists(candidate) ? candidate : null;
    }
}
