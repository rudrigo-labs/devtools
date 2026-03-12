namespace DevTools.Harvest.Models;

public sealed record KeywordDensity(
    string Category,
    int Hits,
    double Density);

public sealed record HarvestHit(
    string File,
    double Score,
    int FanIn,
    int FanOut,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> Reasons,
    IReadOnlyList<KeywordDensity> KeywordDensities);
