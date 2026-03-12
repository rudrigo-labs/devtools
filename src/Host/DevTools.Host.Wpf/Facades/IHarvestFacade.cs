using DevTools.Core.Results;
using DevTools.Core.Validation;
using DevTools.Harvest.Models;

namespace DevTools.Host.Wpf.Facades;

public interface IHarvestFacade
{
    Task<IReadOnlyList<HarvestEntity>> LoadAsync(CancellationToken ct = default);
    Task<ValidationResult> SaveAsync(HarvestEntity entity, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
    Task<RunResult<HarvestResult>> ExecuteAsync(HarvestRequest request, CancellationToken ct = default);
}
