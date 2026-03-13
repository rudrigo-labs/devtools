using DevTools.Core.Abstractions;
using DevTools.SSHTunnel.Models;

namespace DevTools.SSHTunnel.Repositories;

public interface ISshTunnelEntityRepository : IRepository<SshTunnelEntity>
{
    Task<SshTunnelEntity?> GetDefaultAsync(CancellationToken ct = default);
    Task SetDefaultAsync(string id, CancellationToken ct = default);
}
