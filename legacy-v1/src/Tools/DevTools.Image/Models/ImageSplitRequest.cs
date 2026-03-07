namespace DevTools.Image.Models;

public sealed record ImageSplitRequest(
    string InputPath,
    string? OutputDirectory = null,
    string? OutputBaseName = null,
    string? OutputExtension = null,
    byte AlphaThreshold = 10,
    int StartIndex = 1,
    bool Overwrite = false,
    int MinRegionWidth = 3,
    int MinRegionHeight = 3);
