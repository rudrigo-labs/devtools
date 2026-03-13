using DevTools.Core.Validation;
using DevTools.Ngrok.Models;
using DevTools.Ngrok.Repositories;
using DevTools.Ngrok.Validators;

namespace DevTools.Ngrok.Services;

public sealed class NgrokEntityService
{
    private readonly INgrokEntityRepository _repository;
    private readonly IValidator<NgrokEntity> _validator;

    public NgrokEntityService(INgrokEntityRepository repository, IValidator<NgrokEntity>? validator = null)
    {
        _repository = repository;
        _validator  = validator ?? new NgrokEntityValidator();
    }

    public Task<IReadOnlyList<NgrokEntity>> ListAsync(CancellationToken ct = default) =>
        _repository.ListAsync(ct);

    public Task<NgrokEntity?> GetByIdAsync(string id, CancellationToken ct = default) =>
        _repository.GetByIdAsync(id, ct);

    public Task<NgrokEntity?> GetDefaultAsync(CancellationToken ct = default) =>
        _repository.GetDefaultAsync(ct);

    public async Task<ValidationResult> UpsertAsync(NgrokEntity entity, CancellationToken ct = default)
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

    private static void EnsureIdentity(NgrokEntity entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Id))
            entity.Id = Guid.NewGuid().ToString("N");
        if (entity.CreatedAtUtc == default)
            entity.CreatedAtUtc = DateTime.UtcNow;
        entity.UpdatedAtUtc = DateTime.UtcNow;
    }
}
