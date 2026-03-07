namespace DevTools.SearchText.Models;

public sealed record SearchTextResponse(
    string RootPath,
    int TotalFilesScanned,
    int TotalFilesWithMatches,
    int TotalOccurrences,
    IReadOnlyList<SearchTextFileMatch> Files);
