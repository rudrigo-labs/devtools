using DevTools.Core.Validation;
using DevTools.Harvest.Models;
using DevTools.Harvest.Repositories;
using DevTools.Harvest.Validators;

namespace DevTools.Harvest.Services;

public sealed class HarvestEntityService
{
    private readonly IHarvestEntityRepository _repository;
    private readonly IValidator<HarvestEntity> _validator;

    public HarvestEntityService(
        IHarvestEntityRepository repository,
        IValidator<HarvestEntity>? validator = null)
    {
        _repository = repository;
        _validator = validator ?? new HarvestEntityValidator();
    }

    public Task<IReadOnlyList<HarvestEntity>> ListAsync(CancellationToken ct = default) =>
        _repository.ListAsync(ct);

    public Task<HarvestEntity?> GetByIdAsync(string id, CancellationToken ct = default) =>
        _repository.GetByIdAsync(id, ct);

    public Task<HarvestEntity?> GetDefaultAsync(CancellationToken ct = default) =>
        _repository.GetDefaultAsync(ct);

    public async Task<ValidationResult> UpsertAsync(HarvestEntity entity, CancellationToken ct = default)
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

    private static void EnsureIdentity(HarvestEntity entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Id))
            entity.Id = Guid.NewGuid().ToString("N");

        if (entity.CreatedAtUtc == default)
            entity.CreatedAtUtc = DateTime.UtcNow;

        entity.UpdatedAtUtc = DateTime.UtcNow;
    }
}
