namespace DevTools.Rename.Models;

public sealed record RenameChange(
    RenameChangeType Type,
    string Path,
    string? NewPath = null,
    string? BackupPath = null);
