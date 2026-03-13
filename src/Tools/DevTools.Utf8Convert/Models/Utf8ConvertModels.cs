namespace DevTools.Utf8Convert.Models;

public enum Utf8ConvertStatus
{
    Converted = 0,
    AlreadyUtf8 = 1,
    SkippedBinary = 2,
    SkippedExcluded = 3,
    Error = 4
}

public sealed record Utf8ConvertItem(
    string Path,
    Utf8ConvertStatus Status,
    string? DetectedEncoding,
    string? OutputEncoding,
    string? Error);

public sealed record Utf8ConvertSummary(
    int FilesScanned,
    int Converted,
    int AlreadyUtf8,
    int SkippedBinary,
    int SkippedExcluded,
    int Errors);

public sealed record Utf8ConvertResult(
    Utf8ConvertSummary Summary,
    IReadOnlyList<Utf8ConvertItem> Items);

public sealed class Utf8ConvertRequest
{
    public string RootPath { get; set; } = string.Empty;
    public bool Recursive { get; set; } = true;
    public bool DryRun { get; set; } = false;
    public bool CreateBackup { get; set; } = true;
    public bool OutputBom { get; set; } = true;
    public IReadOnlyList<string>? IncludeGlobs { get; set; }
    public IReadOnlyList<string>? ExcludeGlobs { get; set; }
}
