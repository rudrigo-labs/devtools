namespace DevTools.Image.Models;

public sealed record ImageSplitResponse(
    string InputPath,
    string OutputDirectory,
    int TotalComponents,
    IReadOnlyList<ImageSplitOutput> Outputs);
