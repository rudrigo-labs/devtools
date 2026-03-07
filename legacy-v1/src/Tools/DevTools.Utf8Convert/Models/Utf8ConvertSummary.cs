namespace DevTools.Utf8Convert.Models;

public sealed record Utf8ConvertSummary(
    int FilesScanned,
    int Converted,
    int AlreadyUtf8,
    int SkippedBinary,
    int SkippedExcluded,
    int Errors);
