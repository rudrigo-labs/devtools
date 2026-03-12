namespace DevTools.Rename.Models;

public static class RenameDefaults
{
    public static readonly string[] DefaultIgnoredDirectories =
    {
        "bin", "obj", ".git", ".vs", ".vscode",
        "node_modules", "dist", "build", "packages", "out", ".idea"
    };

    public static readonly string[] DefaultIgnoredExtensions =
    {
        ".dll", ".exe", ".pdb", ".obj", ".lib",
        ".png", ".jpg", ".jpeg", ".gif", ".ico", ".bmp", ".svg",
        ".zip", ".rar", ".7z", ".tar", ".gz",
        ".mp3", ".mp4", ".mov", ".avi",
        ".pdf", ".docx", ".xlsx", ".pptx"
    };

    public static readonly string[] DefaultIncludedExtensions =
    {
        ".cs", ".csproj", ".sln", ".props", ".targets",
        ".json", ".xml", ".yaml", ".yml",
        ".md", ".txt", ".config", ".env",
        ".sh", ".bat", ".ps1",
        ".razor", ".cshtml", ".html", ".css", ".js", ".ts"
    };
}
