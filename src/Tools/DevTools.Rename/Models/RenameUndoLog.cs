namespace DevTools.Rename.Models;

public sealed record RenameUndoLog(
    DateTimeOffset GeneratedAt,
    IReadOnlyList<RenameUndoOperation> Operations);

public sealed record RenameUndoOperation(
    RenameChangeType Type,
    string Path,
    string? NewPath,
    string? BackupPath);
