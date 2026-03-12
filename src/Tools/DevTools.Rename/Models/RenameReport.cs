namespace DevTools.Rename.Models;

public sealed record RenameReport(
    DateTimeOffset GeneratedAt,
    RenameRequest Request,
    RenameSummary Summary,
    IReadOnlyList<RenameChange> Changes,
    IReadOnlyList<FileDiffSummary> DiffSummaries,
    IReadOnlyList<RenameReportError> Errors,
    string? UndoLogPath);

public sealed record RenameReportError(
    string Code,
    string Message,
    string? Details);
