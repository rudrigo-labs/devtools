using DevTools.Core.Results;

namespace DevTools.Harvest.Models;

public sealed record HarvestReport(
    string RootPath,
    int TotalFilesAnalyzed,
    int TotalFilesScored,
    IReadOnlyList<HarvestHit> Hits,
    IReadOnlyList<ErrorDetail> Issues);
