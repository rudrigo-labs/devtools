namespace DevTools.Harvest.Configuration;

public static class HarvestDefaults
{
    public static readonly IReadOnlyList<string> DefaultExcludeDirectories = new[]
    {
        "bin",
        "obj",
        ".git",
        ".vs",
        "node_modules",
        "dist",
        "build",
        ".idea",
        ".vscode",
        ".next",
        ".nuxt",
        ".turbo",
        "Snapshot"
    };
}
