using System.IO;
using System.Linq;
using DevTools.Ngrok.Models;

namespace DevTools.Ngrok.Engine;

public sealed class NgrokEnvironmentService
{
    public NgrokEnvironmentInfo Detect(string? configuredExecutablePath = null)
    {
        var executablePath = FindExecutablePath(configuredExecutablePath);
        var configPath = FindConfigPath();
        var token = ReadToken(configPath);

        return new NgrokEnvironmentInfo
        {
            NgrokInstalled = !string.IsNullOrWhiteSpace(executablePath),
            IsConfigured = !string.IsNullOrWhiteSpace(token),
            ExecutablePath = executablePath ?? string.Empty,
            ConfigPath = configPath ?? string.Empty,
            Authtoken = token ?? string.Empty
        };
    }

    private static string? FindExecutablePath(string? configuredExecutablePath)
    {
        if (!string.IsNullOrWhiteSpace(configuredExecutablePath))
        {
            var configured = configuredExecutablePath.Trim();
            if (File.Exists(configured))
            {
                return configured;
            }
        }

        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathEnv))
        {
            return null;
        }

        var candidates = pathEnv
            .Split(Path.PathSeparator)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => path.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .SelectMany(path => new[]
            {
                Path.Combine(path, "ngrok.exe"),
                Path.Combine(path, "ngrok")
            });

        return candidates.FirstOrDefault(File.Exists);
    }

    private static string? FindConfigPath()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrWhiteSpace(userProfile))
        {
            return null;
        }

        var possiblePaths = new[]
        {
            Path.Combine(userProfile, ".ngrok2", "ngrok.yml"),
            Path.Combine(userProfile, "AppData", "Local", "ngrok", "ngrok.yml")
        };

        return possiblePaths.FirstOrDefault(File.Exists);
    }

    private static string? ReadToken(string? configPath)
    {
        if (string.IsNullOrWhiteSpace(configPath) || !File.Exists(configPath))
        {
            return null;
        }

        foreach (var rawLine in File.ReadLines(configPath))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            if (!line.StartsWith("authtoken:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var separator = line.IndexOf(':');
            if (separator < 0 || separator == line.Length - 1)
            {
                return null;
            }

            return line[(separator + 1)..].Trim();
        }

        return null;
    }
}
