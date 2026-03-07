namespace DevTools.Harvest.Models;

public sealed record KeywordDensity(
    string Category,
    int Hits,
    double Density);
