namespace DevTools.Notes.Models;

public sealed record NotesBackupReport(
    int ImportedCount,
    int SkippedCount,
    int ConflictCount,
    IReadOnlyList<string> ConflictFiles);
