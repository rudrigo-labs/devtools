namespace DevTools.SearchText.Engine;

public static class SearchTextDefaults
{
    public static readonly IReadOnlyList<string> DefaultExcludeGlobs =
    [
        "**/.git/**",
        "**/bin/**",
        "**/obj/**",
        "**/.vs/**",
        "**/.idea/**",
        "**/.vscode/**",
        "**/node_modules/**"
    ];
}
