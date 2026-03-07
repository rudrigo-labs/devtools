namespace DevTools.Rename.Models;

public sealed record RenameSummary(
    int FilesScanned,
    int DirectoriesScanned,
    int FilesUpdated,
    int FilesRenamed,
    int DirectoriesRenamed,
    int SkippedBinary,
    int SkippedExists,
    int Errors);
