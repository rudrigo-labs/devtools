using DevTools.Core.Validation;
using DevTools.Organizer.Models;
using DevTools.Organizer.Repositories;
using DevTools.Organizer.Validators;

namespace DevTools.Organizer.Services;

public sealed class OrganizerEntityService
{
    private readonly IOrganizerEntityRepository _repository;
    private readonly IValidator<OrganizerEntity> _validator;

    public OrganizerEntityService(
        IOrganizerEntityRepository repository,
        IValidator<OrganizerEntity>? validator = null)
    {
        _repository = repository;
        _validator = validator ?? new OrganizerEntityValidator();
    }

    public Task<IReadOnlyList<OrganizerEntity>> ListAsync(CancellationToken ct = default) =>
        _repository.ListAsync(ct);

    public Task<OrganizerEntity?> GetByIdAsync(string id, CancellationToken ct = default) =>
        _repository.GetByIdAsync(id, ct);

    public Task<OrganizerEntity?> GetDefaultAsync(CancellationToken ct = default) =>
        _repository.GetDefaultAsync(ct);

    public async Task<ValidationResult> UpsertAsync(OrganizerEntity entity, CancellationToken ct = default)
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

    private static void EnsureIdentity(OrganizerEntity entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Id))
            entity.Id = Guid.NewGuid().ToString("N");

        if (entity.CreatedAtUtc == default)
            entity.CreatedAtUtc = DateTime.UtcNow;

        entity.UpdatedAtUtc = DateTime.UtcNow;
    }
}
