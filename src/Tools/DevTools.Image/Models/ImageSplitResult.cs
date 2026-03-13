namespace DevTools.Image.Models;

public sealed record ImageSplitResult(
    string InputPath,
    string OutputDirectory,
    int TotalComponents,
    IReadOnlyList<ImageSplitOutput> Outputs);
