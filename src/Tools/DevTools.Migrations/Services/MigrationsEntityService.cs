using DevTools.Core.Validation;
using DevTools.Migrations.Models;
using DevTools.Migrations.Repositories;
using DevTools.Migrations.Validators;

namespace DevTools.Migrations.Services;

public sealed class MigrationsEntityService
{
    private readonly IMigrationsEntityRepository _repository;
    private readonly IValidator<MigrationsEntity> _validator;

    public MigrationsEntityService(
        IMigrationsEntityRepository repository,
        IValidator<MigrationsEntity>? validator = null)
    {
        _repository = repository;
        _validator = validator ?? new MigrationsEntityValidator();
    }

    public Task<IReadOnlyList<MigrationsEntity>> ListAsync(CancellationToken ct = default) =>
        _repository.ListAsync(ct);

    public Task<MigrationsEntity?> GetByIdAsync(string id, CancellationToken ct = default) =>
        _repository.GetByIdAsync(id, ct);

    public Task<MigrationsEntity?> GetDefaultAsync(CancellationToken ct = default) =>
        _repository.GetDefaultAsync(ct);

    public async Task<ValidationResult> UpsertAsync(MigrationsEntity entity, CancellationToken ct = default)
    {
        EnsureIdentity(entity);

        var validation = _validator.Validate(entity);
        if (!validation.IsValid)
            return validation;

        await _repository.UpsertAsync(entity, ct);
        return ValidationResult.Success;
    }

    public Task DeleteAsync(string id, CancellationToken ct = default) =>
        _repository.DeleteAsync(id, ct);

    public Task SetDefaultAsync(string id, CancellationToken ct = default) =>
        _repository.SetDefaultAsync(id, ct);

    private static void EnsureIdentity(MigrationsEntity entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Id))
            entity.Id = Guid.NewGuid().ToString("N");

        if (entity.CreatedAtUtc == default)
            entity.CreatedAtUtc = DateTime.UtcNow;

        entity.UpdatedAtUtc = DateTime.UtcNow;
    }
}
