using DevTools.Core.Models;

namespace DevTools.Snapshot.Models;

/// <summary>
/// Request de execução do Snapshot.
/// Herda FileToolOptions — RootPath, IgnoredDirectories, IgnoredExtensions e MaxFileSizeKb vêm da base.
/// </summary>
public sealed class SnapshotRequest : FileToolOptions
{
    public string OutputBasePath { get; set; } = string.Empty;
    public bool GenerateText { get; set; } = true;
    public bool GenerateJsonNested { get; set; }
    public bool GenerateJsonRecursive { get; set; }
    public bool GenerateHtmlPreview { get; set; }

    public SnapshotRequest()
    {
        IgnoredDirectories = SnapshotDefaults.DefaultIgnoredDirectories;
        IgnoredExtensions = SnapshotDefaults.DefaultIgnoredExtensions;
    }
}
