namespace DevTools.Harvest.Models;

internal sealed record FileNode(
    string FullPath,
    string RelativePath,
    string Extension,
    string Content,
    int LineCount,
    string? Namespace,
    IReadOnlyList<string> DeclaredTypes,
    int PublicStaticMethodCount,
    bool IsEntrypointCandidate);
