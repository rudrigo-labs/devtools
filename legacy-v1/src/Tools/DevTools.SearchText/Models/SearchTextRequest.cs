namespace DevTools.SearchText.Models;

public sealed record SearchTextRequest(
    string RootPath,
    string Pattern,
    bool UseRegex = false,
    bool CaseSensitive = false,
    bool WholeWord = false,
    IReadOnlyList<string>? IncludeGlobs = null,
    IReadOnlyList<string>? ExcludeGlobs = null,
    int? MaxFileSizeKb = null,
    bool SkipBinaryFiles = true,
    int MaxMatchesPerFile = 0,
    bool ReturnLines = true);
