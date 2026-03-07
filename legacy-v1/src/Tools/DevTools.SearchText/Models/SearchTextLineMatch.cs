namespace DevTools.SearchText.Models;

public sealed record SearchTextLineMatch(
    int LineNumber,
    string LineText,
    IReadOnlyList<int> Columns);
