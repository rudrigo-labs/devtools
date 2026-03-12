namespace DevTools.Harvest.Models;

public static class HarvestDefaults
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
        ".cs", ".ts", ".js"
    };

    public const double DefaultFanInWeight = 2.0;
    public const double DefaultFanOutWeight = 0.5;
    public const double DefaultKeywordDensityWeight = 1.0;
    public const int DefaultDensityScale = 100;
    public const int DefaultStaticMethodThreshold = 3;
    public const double DefaultStaticMethodBonus = 10.0;
    public const double DefaultDeadCodePenalty = 5.0;
    public const int DefaultLargeFileThresholdLines = 0;
    public const double DefaultLargeFilePenalty = 0;
    public const int DefaultMinScore = 0;
    public const int DefaultTopN = 100;

    public static List<HarvestKeywordCategory> DefaultCategories() =>
    [
        new HarvestKeywordCategory
        {
            Name = "Security",
            Weight = 1.0,
            Keywords = ["encrypt", "decrypt", "aes", "hash", "token", "salt", "jwt", "crypto", "password"]
        },
        new HarvestKeywordCategory
        {
            Name = "Database",
            Weight = 1.0,
            Keywords = ["db", "database", "sql", "query", "connection", "transaction", "repository", "entity", "migrate", "migration"]
        },
        new HarvestKeywordCategory
        {
            Name = "IO",
            Weight = 1.0,
            Keywords = ["file", "stream", "path", "directory", "read", "write", "input", "output"]
        },
        new HarvestKeywordCategory
        {
            Name = "UI",
            Weight = 1.0,
            Keywords = ["view", "window", "form", "button", "textbox", "xaml", "wpf", "winforms", "dialog"]
        }
    ];
}
