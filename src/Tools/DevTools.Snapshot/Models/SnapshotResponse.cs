namespace DevTools.Snapshot.Models;

public sealed record SnapshotResponse(
    string RootPath,
    string OutputBasePath,
    SnapshotStats Stats,
    IReadOnlyList<SnapshotArtifact> Artifacts);
