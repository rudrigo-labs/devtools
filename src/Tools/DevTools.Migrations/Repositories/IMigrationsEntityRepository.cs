using DevTools.Core.Abstractions;
using DevTools.Migrations.Models;

namespace DevTools.Migrations.Repositories;

public interface IMigrationsEntityRepository : IRepository<MigrationsEntity>
{
    Task<MigrationsEntity?> GetDefaultAsync(CancellationToken ct = default);
    Task SetDefaultAsync(string id, CancellationToken ct = default);
}
