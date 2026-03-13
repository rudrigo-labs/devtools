using DevTools.Core.Validation;
using DevTools.Notes.Models;
using DevTools.Notes.Repositories;
using DevTools.Notes.Validators;

namespace DevTools.Notes.Services;

public sealed class NotesEntityService
{
    private readonly INotesEntityRepository _repository;
    private readonly IValidator<NotesEntity> _validator;

    public NotesEntityService(
        INotesEntityRepository repository,
        IValidator<NotesEntity>? validator = null)
    {
        _repository = repository;
        _validator = validator ?? new NotesEntityValidator();
    }

    public Task<IReadOnlyList<NotesEntity>> ListAsync(CancellationToken ct = default) =>
        _repository.ListAsync(ct);

    public Task<NotesEntity?> GetByIdAsync(string id, CancellationToken ct = default) =>
        _repository.GetByIdAsync(id, ct);

    public Task<NotesEntity?> GetDefaultAsync(CancellationToken ct = default) =>
        _repository.GetDefaultAsync(ct);

    public async Task<ValidationResult> UpsertAsync(NotesEntity entity, CancellationToken ct = default)
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

    private static void EnsureIdentity(NotesEntity entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Id))
            entity.Id = Guid.NewGuid().ToString("N");

        if (entity.CreatedAtUtc == default)
            entity.CreatedAtUtc = DateTime.UtcNow;

        entity.UpdatedAtUtc = DateTime.UtcNow;
    }
}
