using System.Text.Json;
using DevTools.Infrastructure.Persistence.Entities;
using DevTools.Snapshot.Models;
using DevTools.Snapshot.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DevTools.Infrastructure.Persistence.Repositories;

public sealed class SnapshotEntityRepository : ISnapshotEntityRepository
{
    private const string ToolSlug = "snapshot";
    private readonly DbContextOptions<DevToolsDbContext> _dbOptions;

    public SnapshotEntityRepository(DbContextOptions<DevToolsDbContext> dbOptions)
    {
        _dbOptions = dbOptions;
    }

    public async Task<IReadOnlyList<SnapshotEntity>> ListAsync(CancellationToken ct = default)
    {
        await using var db = new DevToolsDbContext(_dbOptions);
        var rows = await db.ToolConfigurations
            .Where(x => x.ToolSlug == ToolSlug)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Name)
            .ToListAsync(ct);

        return rows.Select(ToModel).ToList();
    }

    public async Task<SnapshotEntity?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        await using var db = new DevToolsDbContext(_dbOptions);
        var row = await db.ToolConfigurations
            .SingleOrDefaultAsync(x => x.ToolSlug == ToolSlug && x.Id == id, ct);

        return row is null ? null : ToModel(row);
    }

    public async Task UpsertAsync(SnapshotEntity entity, CancellationToken ct = default)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));

        if (string.IsNullOrWhiteSpace(entity.Id))
            throw new InvalidOperationException("SnapshotEntity.Id e obrigatorio para persistencia.");

        await using var db = new DevToolsDbContext(_dbOptions);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var row = await db.ToolConfigurations
            .SingleOrDefaultAsync(x => x.ToolSlug == ToolSlug && x.Id == entity.Id, ct);

        if (entity.IsDefault)
        {
            await db.ToolConfigurations
                .Where(x => x.ToolSlug == ToolSlug && x.Id != entity.Id && x.IsDefault)
                .ExecuteUpdateAsync(x => x.SetProperty(e => e.IsDefault, false), ct);
        }

        if (row is null)
        {
            row = new ToolConfigurationEntity
            {
                Id = entity.Id,
                ToolSlug = ToolSlug,
                Name = entity.Name,
                Description = entity.Description,
                IsActive = entity.IsActive,
                IsDefault = entity.IsDefault,
                PayloadJson = SerializePayload(entity),
                CreatedAtUtc = entity.CreatedAtUtc == default ? DateTime.UtcNow : entity.CreatedAtUtc,
                UpdatedAtUtc = entity.UpdatedAtUtc == default ? DateTime.UtcNow : entity.UpdatedAtUtc
            };

            db.ToolConfigurations.Add(row);
        }
        else
        {
            row.Name = entity.Name;
            row.Description = entity.Description;
            row.IsActive = entity.IsActive;
            row.IsDefault = entity.IsDefault;
            row.PayloadJson = SerializePayload(entity);
            row.UpdatedAtUtc = entity.UpdatedAtUtc == default ? DateTime.UtcNow : entity.UpdatedAtUtc;
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            return;

        await using var db = new DevToolsDbContext(_dbOptions);
        var row = await db.ToolConfigurations
            .SingleOrDefaultAsync(x => x.ToolSlug == ToolSlug && x.Id == id, ct);

        if (row is null)
            return;

        db.ToolConfigurations.Remove(row);
        await db.SaveChangesAsync(ct);
    }

    public async Task<SnapshotEntity?> GetDefaultAsync(CancellationToken ct = default)
    {
        await using var db = new DevToolsDbContext(_dbOptions);
        var row = await db.ToolConfigurations
            .Where(x => x.ToolSlug == ToolSlug && x.IsDefault)
            .OrderBy(x => x.Name)
            .FirstOrDefaultAsync(ct);

        return row is null ? null : ToModel(row);
    }

    public async Task SetDefaultAsync(string id, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Id e obrigatorio.", nameof(id));

        await using var db = new DevToolsDbContext(_dbOptions);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var target = await db.ToolConfigurations
            .SingleOrDefaultAsync(x => x.ToolSlug == ToolSlug && x.Id == id, ct);

        if (target is null)
            throw new InvalidOperationException($"Configuracao Snapshot '{id}' nao encontrada.");

        await db.ToolConfigurations
            .Where(x => x.ToolSlug == ToolSlug && x.IsDefault)
            .ExecuteUpdateAsync(x => x.SetProperty(e => e.IsDefault, false), ct);

        target.IsDefault = true;
        target.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    private static SnapshotEntity ToModel(ToolConfigurationEntity entity)
    {
        var payload = DeserializePayload(entity.PayloadJson);
        return new SnapshotEntity
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            IsActive = entity.IsActive,
            IsDefault = entity.IsDefault,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc,
            RootPath = payload.RootPath,
            OutputBasePath = payload.OutputBasePath,
            GenerateText = payload.GenerateText,
            GenerateJsonNested = payload.GenerateJsonNested,
            GenerateJsonRecursive = payload.GenerateJsonRecursive,
            GenerateHtmlPreview = payload.GenerateHtmlPreview,
            IgnoredDirectories = payload.IgnoredDirectories.Length == 0
                ? SnapshotDefaults.DefaultIgnoredDirectories
                : payload.IgnoredDirectories,
            IgnoredExtensions = payload.IgnoredExtensions.Length == 0
                ? SnapshotDefaults.DefaultIgnoredExtensions
                : payload.IgnoredExtensions,
            MaxFileSizeKb = payload.MaxFileSizeKb is > 0
                ? payload.MaxFileSizeKb
                : null
        };
    }

    private static string SerializePayload(SnapshotEntity model)
    {
        var payload = new SnapshotPayload
        {
            RootPath = model.RootPath,
            OutputBasePath = model.OutputBasePath,
            GenerateText = model.GenerateText,
            GenerateJsonNested = model.GenerateJsonNested,
            GenerateJsonRecursive = model.GenerateJsonRecursive,
            GenerateHtmlPreview = model.GenerateHtmlPreview,
            IgnoredDirectories = model.IgnoredDirectories?.ToArray() ?? SnapshotDefaults.DefaultIgnoredDirectories,
            IgnoredExtensions = model.IgnoredExtensions?.ToArray() ?? SnapshotDefaults.DefaultIgnoredExtensions,
            MaxFileSizeKb = null
        };

        return JsonSerializer.Serialize(payload);
    }

    private static SnapshotPayload DeserializePayload(string payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
            return new SnapshotPayload();

        return JsonSerializer.Deserialize<SnapshotPayload>(payloadJson)
            ?? new SnapshotPayload();
    }

    private sealed class SnapshotPayload
    {
        public string RootPath { get; set; } = string.Empty;
        public string OutputBasePath { get; set; } = string.Empty;
        public bool GenerateText { get; set; } = true;
        public bool GenerateJsonNested { get; set; }
        public bool GenerateJsonRecursive { get; set; }
        public bool GenerateHtmlPreview { get; set; }
        public string[] IgnoredDirectories { get; set; } = Array.Empty<string>();
        public string[] IgnoredExtensions { get; set; } = Array.Empty<string>();
        public int? MaxFileSizeKb { get; set; }
    }
}
