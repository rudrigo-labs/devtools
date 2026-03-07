namespace DevTools.Rename.Models;

public enum RenameChangeType
{
    ContentUpdated = 0,
    FileRenamed = 1,
    DirectoryRenamed = 2,
    SkippedExists = 3,
    SkippedBinary = 4,
    SkippedNoMatch = 5
}
