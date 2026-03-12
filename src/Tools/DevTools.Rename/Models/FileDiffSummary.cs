namespace DevTools.Rename.Models;

public sealed record FileDiffSummary(
    string Path,
    IReadOnlyList<LineChange> Changes);
