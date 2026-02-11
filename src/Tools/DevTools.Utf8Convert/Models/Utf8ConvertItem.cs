namespace DevTools.Utf8Convert.Models;

public sealed record Utf8ConvertItem(
    string Path,
    Utf8ConvertStatus Status,
    string? DetectedEncoding,
    string? OutputEncoding,
    string? Error);
