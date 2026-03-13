namespace DevTools.SearchText.Models;

public sealed class SearchTextRequest
{
    public string RootPath { get; set; } = string.Empty;
    public string Pattern { get; set; } = string.Empty;
    public bool UseRegex { get; set; } = false;
    public bool CaseSensitive { get; set; } = false;
    public bool WholeWord { get; set; } = false;
    public IReadOnlyList<string>? IncludeGlobs { get; set; }
    public IReadOnlyList<string>? ExcludeGlobs { get; set; }
    public int? MaxFileSizeKb { get; set; }
    public bool SkipBinaryFiles { get; set; } = true;
    public int MaxMatchesPerFile { get; set; } = 0;
    public bool ReturnLines { get; set; } = true;
}
