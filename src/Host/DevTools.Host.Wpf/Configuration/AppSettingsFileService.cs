using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using DevTools.Core.Models;

namespace DevTools.Host.Wpf.Configuration;

public sealed class AppSettingsFileService
{
    private const string FileName = "appsettings.json";
    private static readonly JsonSerializerOptions WriteJsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _baseDirectory;

    public AppSettingsFileService()
        : this(AppContext.BaseDirectory)
    {
    }

    public AppSettingsFileService(string baseDirectory)
    {
        _baseDirectory = string.IsNullOrWhiteSpace(baseDirectory)
            ? AppContext.BaseDirectory
            : baseDirectory;
    }

    public AppSettingsFileSnapshot Load()
    {
        var path = GetSettingsFilePath();
        var fileExists = File.Exists(path);
        var parseError = false;

        var root = new JsonObject();
        if (fileExists)
        {
            try
            {
                root = JsonNode.Parse(File.ReadAllText(path)) as JsonObject ?? new JsonObject();
            }
            catch
            {
                parseError = true;
                root = new JsonObject();
            }
        }

        var settings = ParseSettings(root);
        return new AppSettingsFileSnapshot(path, fileExists, parseError, settings);
    }

    public void SaveFileToolsSettings(int maxFileSizeKb, int absoluteMaxFileSizeKb)
    {
        var current = Load().Settings;
        SaveGeneralSettings(
            maxFileSizeKb,
            absoluteMaxFileSizeKb,
            current.FileTools.DefaultIncludeGlobs,
            current.FileTools.DefaultExcludeGlobs,
            current.History.Enabled,
            current.ToolVisibility.DisabledTools);
    }

    public void SaveGeneralSettings(
        int maxFileSizeKb,
        int absoluteMaxFileSizeKb,
        IReadOnlyCollection<string> defaultIncludeGlobs,
        IReadOnlyCollection<string> defaultExcludeGlobs,
        bool historyEnabled,
        IReadOnlyCollection<string> disabledTools)
    {
        if (maxFileSizeKb <= 0)
            throw new InvalidOperationException("MaxFileSizeKb deve ser maior que zero.");

        if (absoluteMaxFileSizeKb <= 0)
            throw new InvalidOperationException("AbsoluteMaxFileSizeKb deve ser maior que zero.");

        if (absoluteMaxFileSizeKb < maxFileSizeKb)
            throw new InvalidOperationException("AbsoluteMaxFileSizeKb deve ser maior ou igual a MaxFileSizeKb.");

        var normalizedIncludeGlobs = NormalizeGlobs(defaultIncludeGlobs, fallbackWhenEmpty: ["**/*"]);
        var normalizedExcludeGlobs = NormalizeGlobs(defaultExcludeGlobs);
        var normalizedDisabledTools = (disabledTools ?? Array.Empty<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var path = GetSettingsFilePath();
        var root = LoadRootForSave(path);

        var devTools = EnsureObject(root, "DevTools");
        var fileTools = EnsureObject(devTools, "FileTools");
        fileTools["MaxFileSizeKb"] = maxFileSizeKb;
        fileTools["AbsoluteMaxFileSizeKb"] = absoluteMaxFileSizeKb;
        fileTools["DefaultIncludeGlobs"] = ToJsonArray(normalizedIncludeGlobs);
        fileTools["DefaultExcludeGlobs"] = ToJsonArray(normalizedExcludeGlobs);

        var history = EnsureObject(devTools, "History");
        history["Enabled"] = historyEnabled;

        var toolVisibility = EnsureObject(devTools, "ToolVisibility");
        var disabledToolsArray = new JsonArray();
        foreach (var tool in normalizedDisabledTools)
            disabledToolsArray.Add(tool);

        toolVisibility["DisabledTools"] = disabledToolsArray;

        var json = root.ToJsonString(WriteJsonOptions) + Environment.NewLine;
        SaveAtomically(path, json);
    }

    private string GetSettingsFilePath()
        => Path.Combine(_baseDirectory, FileName);

    private static AppSettings ParseSettings(JsonObject root)
    {
        var result = new AppSettings();

        if (!root.TryGetPropertyValue("DevTools", out var devToolsNode) || devToolsNode is not JsonObject devTools)
            return result;

        if (devTools.TryGetPropertyValue("FileTools", out var fileToolsNode) && fileToolsNode is JsonObject fileTools)
        {
            var max = TryGetPositiveInt(fileTools, "MaxFileSizeKb");
            if (max.HasValue)
                result.FileTools.MaxFileSizeKb = max.Value;

            var absolute = TryGetPositiveInt(fileTools, "AbsoluteMaxFileSizeKb");
            if (absolute.HasValue)
                result.FileTools.AbsoluteMaxFileSizeKb = absolute.Value;

            var includeGlobs = TryGetStringArray(fileTools, "DefaultIncludeGlobs");
            if (includeGlobs.Count > 0)
                result.FileTools.DefaultIncludeGlobs = includeGlobs;

            var excludeGlobs = TryGetStringArray(fileTools, "DefaultExcludeGlobs");
            result.FileTools.DefaultExcludeGlobs = excludeGlobs;
        }

        if (devTools.TryGetPropertyValue("History", out var historyNode) &&
            historyNode is JsonObject history &&
            history.TryGetPropertyValue("Enabled", out var historyEnabledNode) &&
            historyEnabledNode is not null)
        {
            try
            {
                result.History.Enabled = historyEnabledNode.GetValue<bool>();
            }
            catch
            {
                // Ignora valores inválidos e mantém o padrão.
            }
        }

        if (devTools.TryGetPropertyValue("ToolVisibility", out var toolVisibilityNode) &&
            toolVisibilityNode is JsonObject toolVisibility &&
            toolVisibility.TryGetPropertyValue("DisabledTools", out var disabledToolsNode) &&
            disabledToolsNode is JsonArray disabledToolsArray)
        {
            result.ToolVisibility.DisabledTools = disabledToolsArray
                .Select(node => node?.GetValue<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        return result;
    }

    private static List<string> TryGetStringArray(JsonObject parent, string propertyName)
    {
        if (!parent.TryGetPropertyValue(propertyName, out var node) || node is not JsonArray array)
            return [];

        return array
            .Select(x =>
            {
                try { return x?.GetValue<string>(); }
                catch { return null; }
            })
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string[] NormalizeGlobs(
        IReadOnlyCollection<string>? globs,
        IReadOnlyCollection<string>? fallbackWhenEmpty = null)
    {
        var normalized = (globs ?? Array.Empty<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalized.Length > 0)
            return normalized;

        return (fallbackWhenEmpty ?? Array.Empty<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static JsonArray ToJsonArray(IEnumerable<string> values)
    {
        var array = new JsonArray();
        foreach (var value in values)
            array.Add(value);

        return array;
    }

    private static int? TryGetPositiveInt(JsonObject node, string property)
    {
        if (!node.TryGetPropertyValue(property, out var value) || value is null)
            return null;

        try
        {
            var parsed = value.GetValue<int>();
            return parsed > 0 ? parsed : null;
        }
        catch
        {
            return null;
        }
    }

    private static JsonObject LoadRootForSave(string path)
    {
        if (!File.Exists(path))
            return new JsonObject();

        try
        {
            return JsonNode.Parse(File.ReadAllText(path)) as JsonObject ?? new JsonObject();
        }
        catch
        {
            return new JsonObject();
        }
    }

    private static JsonObject EnsureObject(JsonObject parent, string property)
    {
        if (parent.TryGetPropertyValue(property, out var node) && node is JsonObject obj)
            return obj;

        var created = new JsonObject();
        parent[property] = created;
        return created;
    }

    private static void SaveAtomically(string path, string content)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var tempPath = path + ".tmp";
        File.WriteAllText(tempPath, content);
        File.Move(tempPath, path, true);
    }
}

public sealed record AppSettingsFileSnapshot(
    string FilePath,
    bool FileExists,
    bool ParseError,
    AppSettings Settings);
