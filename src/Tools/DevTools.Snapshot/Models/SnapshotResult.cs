namespace DevTools.Snapshot.Models;

public sealed class SnapshotResult
{
    public string RootPath { get; init; } = string.Empty;
    public IReadOnlyList<string> GeneratedArtifacts { get; init; } = Array.Empty<string>();
    public int TotalFilesScanned { get; init; }
    public int TotalFilesIncluded { get; init; }
}
