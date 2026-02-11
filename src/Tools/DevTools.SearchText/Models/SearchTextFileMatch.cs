namespace DevTools.SearchText.Models;

public sealed record SearchTextFileMatch(
    string FullPath,
    string RelativePath,
    int Occurrences,
    IReadOnlyList<SearchTextLineMatch> Lines);
