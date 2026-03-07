namespace DevTools.Snapshot.Models;

public sealed class SnapshotExecutionRequest
{
    public string RootPath { get; set; } = string.Empty;
    public string OutputBasePath { get; set; } = string.Empty;
    public bool GenerateText { get; set; } = true;
    public bool GenerateJsonNested { get; set; }
    public bool GenerateJsonRecursive { get; set; }
    public bool GenerateHtmlPreview { get; set; }
    public IReadOnlyList<string> IgnoredDirectories { get; set; } = Array.Empty<string>();
    public int? MaxFileSizeKb { get; set; }
}

