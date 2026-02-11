namespace DevTools.Rename.Models;

public sealed record RenameRequest(
    string RootPath,
    string OldText,
    string NewText,
    RenameMode Mode = RenameMode.General,
    bool DryRun = false,
    IReadOnlyList<string>? IncludeGlobs = null,
    IReadOnlyList<string>? ExcludeGlobs = null,
    bool BackupEnabled = true,
    bool WriteUndoLog = true,
    string? UndoLogPath = null,
    string? ReportPath = null,
    int MaxDiffLinesPerFile = 200);
