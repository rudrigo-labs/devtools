namespace DevTools.Rename.Models;

public sealed record LineChange(
    int LineNumber,
    string? OldLine,
    string? NewLine);
