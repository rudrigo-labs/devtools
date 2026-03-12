using DevTools.Core.Abstractions;
using DevTools.Harvest.Models;

namespace DevTools.Harvest.Repositories;

public interface IHarvestEntityRepository : IRepository<HarvestEntity>
{
    Task<HarvestEntity?> GetDefaultAsync(CancellationToken ct = default);
    Task SetDefaultAsync(string id, CancellationToken ct = default);
}
