namespace DevTools.Notes.Providers;

public static class NotesPaths
{
    public static string ResolveRoot(string? rootPath)
    {
        if (!string.IsNullOrWhiteSpace(rootPath))
            return Path.GetFullPath(rootPath);

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "DevTools", "Notes");
    }

    public static string ItemsDir(string root) => Path.Combine(root, "items");
    public static string ExportsDir(string root) => Path.Combine(root, "exports");
    public static string IndexPath(string root) => Path.Combine(root, "index.json");
}
