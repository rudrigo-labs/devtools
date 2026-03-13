namespace DevTools.SearchText.Models;

public sealed record SearchTextLineMatch(
    int LineNumber,
    string LineText,
    IReadOnlyList<int> Columns);

public sealed record SearchTextFileMatch(
    string FullPath,
    string RelativePath,
    int Occurrences,
    IReadOnlyList<SearchTextLineMatch> Lines);

public sealed record SearchTextResult(
    string RootPath,
    int TotalFilesScanned,
    int TotalFilesWithMatches,
    int TotalOccurrences,
    IReadOnlyList<SearchTextFileMatch> Files);
