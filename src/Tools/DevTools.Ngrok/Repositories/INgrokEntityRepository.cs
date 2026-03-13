using DevTools.Core.Abstractions;
using DevTools.Ngrok.Models;

namespace DevTools.Ngrok.Repositories;

public interface INgrokEntityRepository : IRepository<NgrokEntity>
{
    Task<NgrokEntity?> GetDefaultAsync(CancellationToken ct = default);
    Task SetDefaultAsync(string id, CancellationToken ct = default);
}
