namespace DevTools.Rename.Models;

public sealed record RenameResponse(
    RenameSummary Summary,
    IReadOnlyList<RenameChange> Changes,
    IReadOnlyList<FileDiffSummary> DiffSummaries,
    string? ReportPath,
    string? UndoLogPath);
