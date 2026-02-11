namespace DevTools.Harvest.Models;

public sealed record HarvestHit(
    string File,
    double Score,
    int FanIn,
    int FanOut,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> Reasons,
    IReadOnlyList<KeywordDensity> KeywordDensities);
