using DevTools.Core.Validation;
using DevTools.SSHTunnel.Models;
using DevTools.SSHTunnel.Repositories;
using DevTools.SSHTunnel.Validators;

namespace DevTools.SSHTunnel.Services;

public sealed class SshTunnelEntityService
{
    private readonly ISshTunnelEntityRepository _repository;
    private readonly IValidator<SshTunnelEntity> _validator;

    public SshTunnelEntityService(
        ISshTunnelEntityRepository repository,
        IValidator<SshTunnelEntity>? validator = null)
    {
        _repository = repository;
        _validator = validator ?? new SshTunnelEntityValidator();
    }

    public Task<IReadOnlyList<SshTunnelEntity>> ListAsync(CancellationToken ct = default) =>
        _repository.ListAsync(ct);

    public Task<SshTunnelEntity?> GetByIdAsync(string id, CancellationToken ct = default) =>
        _repository.GetByIdAsync(id, ct);

    public Task<SshTunnelEntity?> GetDefaultAsync(CancellationToken ct = default) =>
        _repository.GetDefaultAsync(ct);

    public async Task<ValidationResult> UpsertAsync(SshTunnelEntity entity, CancellationToken ct = default)
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

    private static void EnsureIdentity(SshTunnelEntity entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Id))
            entity.Id = Guid.NewGuid().ToString("N");

        if (entity.CreatedAtUtc == default)
            entity.CreatedAtUtc = DateTime.UtcNow;

        entity.UpdatedAtUtc = DateTime.UtcNow;
    }
}
