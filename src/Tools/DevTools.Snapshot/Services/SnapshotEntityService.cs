using DevTools.Core.Validation;
using DevTools.Snapshot.Models;
using DevTools.Snapshot.Repositories;
using DevTools.Snapshot.Validators;

namespace DevTools.Snapshot.Services;

public sealed class SnapshotEntityService
{
    private readonly ISnapshotEntityRepository _repository;
    private readonly IValidator<SnapshotEntity> _validator;

    public SnapshotEntityService(
        ISnapshotEntityRepository repository,
        IValidator<SnapshotEntity>? validator = null)
    {
        _repository = repository;
        _validator = validator ?? new SnapshotConfigurationValidator();
    }

    public Task<IReadOnlyList<SnapshotEntity>> ListAsync(CancellationToken ct = default) =>
        _repository.ListAsync(ct);

    public Task<SnapshotEntity?> GetByIdAsync(string id, CancellationToken ct = default) =>
        _repository.GetByIdAsync(id, ct);

    public Task<SnapshotEntity?> GetDefaultAsync(CancellationToken ct = default) =>
        _repository.GetDefaultAsync(ct);

    public async Task<ValidationResult> UpsertAsync(SnapshotEntity configuration, CancellationToken ct = default)
    {
        EnsureIdentity(configuration);

        var validation = _validator.Validate(configuration);
        if (!validation.IsValid)
            return validation;

        // UpsertAsync no repositório já gerencia IsDefault dentro de uma única transação.
        await _repository.UpsertAsync(configuration, ct);
        return ValidationResult.Success;
    }

    public Task DeleteAsync(string id, CancellationToken ct = default) =>
        _repository.DeleteAsync(id, ct);

    public Task SetDefaultAsync(string id, CancellationToken ct = default) =>
        _repository.SetDefaultAsync(id, ct);

    private static void EnsureIdentity(SnapshotEntity configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration.Id))
        {
            // Usa GUID estável como PK para evitar colisão ao renomear configurações.
            configuration.Id = Guid.NewGuid().ToString("N");
        }

        if (configuration.CreatedAtUtc == default)
            configuration.CreatedAtUtc = DateTime.UtcNow;

        configuration.UpdatedAtUtc = DateTime.UtcNow;
    }
}

