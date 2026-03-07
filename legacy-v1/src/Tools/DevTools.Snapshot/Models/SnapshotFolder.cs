namespace DevTools.Snapshot.Models;

public sealed record SnapshotFolder(
    string Name,
    IReadOnlyList<string> Files);
