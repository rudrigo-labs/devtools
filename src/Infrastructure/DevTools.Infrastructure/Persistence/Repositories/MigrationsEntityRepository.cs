using System.Text.Json;
using DevTools.Migrations.Models;
using DevTools.Migrations.Repositories;
using DevTools.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevTools.Infrastructure.Persistence.Repositories;

public sealed class MigrationsEntityRepository : IMigrationsEntityRepository
{
    private const string ToolSlug = "migrations";
    private readonly DbContextOptions<DevToolsDbContext> _dbOptions;

    public MigrationsEntityRepository(DbContextOptions<DevToolsDbContext> dbOptions)
    {
        _dbOptions = dbOptions;
    }

    public async Task<IReadOnlyList<MigrationsEntity>> ListAsync(CancellationToken ct = default)
    {
        await using var db = new DevToolsDbContext(_dbOptions);
        var rows = await db.ToolConfigurations
            .Where(x => x.ToolSlug == ToolSlug)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Name)
            .ToListAsync(ct);

        return rows.Select(ToModel).ToList();
    }

    public async Task<MigrationsEntity?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;

        await using var db = new DevToolsDbContext(_dbOptions);
        var row = await db.ToolConfigurations
            .SingleOrDefaultAsync(x => x.ToolSlug == ToolSlug && x.Id == id, ct);

        return row is null ? null : ToModel(row);
    }

    public async Task<MigrationsEntity?> GetDefaultAsync(CancellationToken ct = default)
    {
        await using var db = new DevToolsDbContext(_dbOptions);
        var row = await db.ToolConfigurations
            .Where(x => x.ToolSlug == ToolSlug && x.IsDefault)
            .FirstOrDefaultAsync(ct);

        return row is null ? null : ToModel(row);
    }

    public async Task UpsertAsync(MigrationsEntity entity, CancellationToken ct = default)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Id))
            throw new InvalidOperationException("MigrationsEntity.Id é obrigatório para persistência.");

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

        var payload = SerializePayload(entity);

        if (row is null)
        {
            db.ToolConfigurations.Add(new ToolConfigurationEntity
            {
                Id          = entity.Id,
                ToolSlug    = ToolSlug,
                Name        = entity.Name,
                Description = entity.Description,
                IsDefault   = entity.IsDefault,
                IsActive    = entity.IsActive,
                PayloadJson = payload,
                CreatedAtUtc = entity.CreatedAtUtc,
                UpdatedAtUtc = entity.UpdatedAtUtc
            });
        }
        else
        {
            row.Name        = entity.Name;
            row.Description = entity.Description;
            row.IsDefault   = entity.IsDefault;
            row.IsActive    = entity.IsActive;
            row.PayloadJson = payload;
            row.UpdatedAtUtc = entity.UpdatedAtUtc;
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        await using var db = new DevToolsDbContext(_dbOptions);
        await db.ToolConfigurations
            .Where(x => x.ToolSlug == ToolSlug && x.Id == id)
            .ExecuteDeleteAsync(ct);
    }

    public async Task SetDefaultAsync(string id, CancellationToken ct = default)
    {
        await using var db = new DevToolsDbContext(_dbOptions);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        await db.ToolConfigurations
            .Where(x => x.ToolSlug == ToolSlug && x.IsDefault)
            .ExecuteUpdateAsync(x => x.SetProperty(e => e.IsDefault, false), ct);

        await db.ToolConfigurations
            .Where(x => x.ToolSlug == ToolSlug && x.Id == id)
            .ExecuteUpdateAsync(x => x.SetProperty(e => e.IsDefault, true), ct);

        await tx.CommitAsync(ct);
    }

    private static MigrationsEntity ToModel(ToolConfigurationEntity row)
    {
        var p = DeserializePayload(row.PayloadJson);
        return new MigrationsEntity
        {
            Id                 = row.Id,
            Name               = row.Name,
            Description        = row.Description,
            IsDefault          = row.IsDefault,
            IsActive           = row.IsActive,
            CreatedAtUtc       = row.CreatedAtUtc,
            UpdatedAtUtc       = row.UpdatedAtUtc,
            RootPath           = p.RootPath,
            StartupProjectPath = p.StartupProjectPath,
            DbContextFullName  = p.DbContextFullName,
            AdditionalArgs     = p.AdditionalArgs,
            Targets            = p.Targets ?? new List<MigrationTarget>()
        };
    }

    private static string SerializePayload(MigrationsEntity model)
    {
        var payload = new MigrationsPayload
        {
            RootPath           = model.RootPath,
            StartupProjectPath = model.StartupProjectPath,
            DbContextFullName  = model.DbContextFullName,
            AdditionalArgs     = model.AdditionalArgs,
            Targets            = model.Targets
        };
        return JsonSerializer.Serialize(payload);
    }

    private static MigrationsPayload DeserializePayload(string payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
            return new MigrationsPayload();

        return JsonSerializer.Deserialize<MigrationsPayload>(payloadJson) ?? new MigrationsPayload();
    }

    private sealed class MigrationsPayload
    {
        public string RootPath { get; set; } = string.Empty;
        public string StartupProjectPath { get; set; } = string.Empty;
        public string DbContextFullName { get; set; } = string.Empty;
        public string? AdditionalArgs { get; set; }
        public List<MigrationTarget> Targets { get; set; } = new();
    }
}
