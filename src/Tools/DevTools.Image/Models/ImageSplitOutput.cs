namespace DevTools.Image.Models;

public sealed record ImageSplitOutput(
    int Index,
    string Path,
    ImageRegion Region);
