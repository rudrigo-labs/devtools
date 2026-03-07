namespace DevTools.Snapshot.Models;

public sealed record SnapshotRequest(
    string RootPath,
    string? OutputBasePath = null,
    bool GenerateText = true,
    bool GenerateJsonNested = false,
    bool GenerateJsonRecursive = false,
    bool GenerateHtmlPreview = false,
    IReadOnlyList<string>? IgnoredDirectories = null,
    int? MaxFileSizeKb = null);
