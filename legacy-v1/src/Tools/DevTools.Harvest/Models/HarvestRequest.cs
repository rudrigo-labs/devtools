namespace DevTools.Harvest.Models;

public sealed record HarvestRequest(
    string RootPath,
    string? OutputPath,
    string? ConfigPath = null,
    int? MinScore = null,
    bool CopyFiles = true);
