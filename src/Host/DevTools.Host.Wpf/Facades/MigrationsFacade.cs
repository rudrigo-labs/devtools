using DevTools.Core.Results;
using DevTools.Core.Validation;
using DevTools.Migrations.Engine;
using DevTools.Migrations.Models;
using DevTools.Migrations.Services;

namespace DevTools.Host.Wpf.Facades;

public interface IMigrationsFacade
{
    Task<IReadOnlyList<MigrationsEntity>> LoadAsync(CancellationToken ct = default);
    Task<ValidationResult> SaveAsync(MigrationsEntity entity, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
    Task<RunResult<MigrationsResult>> ExecuteAsync(MigrationsRequest request, CancellationToken ct = default);
}

public sealed class MigrationsFacade : IMigrationsFacade
{
    private readonly MigrationsEntityService _entityService;
    private readonly MigrationsEngine _engine;

    public MigrationsFacade(MigrationsEntityService entityService, MigrationsEngine engine)
    {
        _entityService = entityService;
        _engine        = engine;
    }

    public Task<IReadOnlyList<MigrationsEntity>> LoadAsync(CancellationToken ct = default) =>
        _entityService.ListAsync(ct);

    public Task<ValidationResult> SaveAsync(MigrationsEntity entity, CancellationToken ct = default) =>
        _entityService.UpsertAsync(entity, ct);

    public Task DeleteAsync(string id, CancellationToken ct = default) =>
        _entityService.DeleteAsync(id, ct);

    public Task<RunResult<MigrationsResult>> ExecuteAsync(MigrationsRequest request, CancellationToken ct = default) =>
        _engine.ExecuteAsync(request, cancellationToken: ct);
}
