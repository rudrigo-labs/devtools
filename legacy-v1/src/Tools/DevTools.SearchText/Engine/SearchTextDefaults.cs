namespace DevTools.SearchText.Engine;

public static class SearchTextDefaults
{
    public static readonly List<string> DefaultExcludeGlobs = new()
    {
        "**/.git/**",
        "**/bin/**",
        "**/obj/**",
        "**/.vs/**",
        "**/.idea/**",
        "**/.vscode/**",
        "**/node_modules/**"
    };
}
