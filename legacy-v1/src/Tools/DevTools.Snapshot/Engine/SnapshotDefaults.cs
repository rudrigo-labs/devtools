namespace DevTools.Snapshot.Engine;

internal static class SnapshotDefaults
{
    public static readonly HashSet<string> IgnoredDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin",
        "obj",
        ".git",
        ".vs",
        ".idea",
        ".vscode",
        "node_modules",
        "dist",
        "build",
        ".next",
        ".nuxt",
        ".turbo",
        "Snapshot"
    };

    public static readonly HashSet<string> PreviewExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".csx", ".json", ".xml", ".config",
        ".csproj", ".sln", ".props", ".targets",
        ".yml", ".yaml",
        ".md", ".txt",
        ".editorconfig", ".gitignore", ".gitattributes",
        ".env", ".ini", ".sql", ".http", ".dockerignore",
        ".html", ".htm",
        ".css", ".scss", ".sass", ".less",
        ".js", ".mjs", ".cjs",
        ".ts", ".tsx", ".jsx",
        ".cshtml", ".razor",
        ".ps1", ".sh", ".bat",
        ".graphql", ".proto"
    };

    public static readonly HashSet<string> TextFilesWithoutExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        "Dockerfile",
        "README",
        "LICENSE",
        "Makefile"
    };

    public static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".gif", ".svg", ".webp", ".ico"
    };

    public static readonly HashSet<string> BinaryExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".dll", ".exe", ".pdb", ".zip", ".7z", ".tar", ".gz", ".pdf", ".db",
        ".woff", ".woff2", ".eot"
    };
}
