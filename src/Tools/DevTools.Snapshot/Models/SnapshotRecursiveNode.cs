namespace DevTools.Snapshot.Models;

public sealed record SnapshotRecursiveNode(
    string Name,
    SnapshotFileKind Kind,
    IReadOnlyList<SnapshotRecursiveNode>? Children);
