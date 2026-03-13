using System.Text.Json;
using DevTools.Infrastructure.Persistence.Entities;
using DevTools.Ngrok.Models;
using DevTools.Ngrok.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DevTools.Infrastructure.Persistence.Repositories;

public sealed class NgrokEntityRepository : INgrokEntityRepository
{
    private const string ToolSlug = "ngrok";
    private readonly DbContextOptions<DevToolsDbContext> _dbOptions;

    public NgrokEntityRepository(DbContextOptions<DevToolsDbContext> dbOptions)
    {
        _dbOptions = dbOptions;
    }

    public async Task<IReadOnlyList<NgrokEntity>> ListAsync(CancellationToken ct = default)
    {
        await using var db = new DevToolsDbContext(_dbOptions);
        var rows = await db.ToolConfigurations
            .Where(x => x.ToolSlug == ToolSlug)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Name)
            .ToListAsync(ct);
        return rows.Select(ToModel).ToList();
    }

    public async Task<NgrokEntity?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        await using var db = new DevToolsDbContext(_dbOptions);
        var row = await db.ToolConfigurations
            .SingleOrDefaultAsync(x => x.ToolSlug == ToolSlug && x.Id == id, ct);
        return row is null ? null : ToModel(row);
    }

    public async Task<NgrokEntity?> GetDefaultAsync(CancellationToken ct = default)
    {
        await using var db = new DevToolsDbContext(_dbOptions);
        var row = await db.ToolConfigurations
            .Where(x => x.ToolSlug == ToolSlug && x.IsDefault)
            .FirstOrDefaultAsync(ct);
        return row is null ? null : ToModel(row);
    }

    public async Task UpsertAsync(NgrokEntity entity, CancellationToken ct = default)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Id))
            throw new InvalidOperationException("NgrokEntity.Id é obrigatório.");

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
                Id           = entity.Id,
                ToolSlug     = ToolSlug,
                Name         = entity.Name,
                Description  = entity.Description,
                IsDefault    = entity.IsDefault,
                IsActive     = entity.IsActive,
                PayloadJson  = payload,
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

    private static NgrokEntity ToModel(ToolConfigurationEntity row)
    {
        var p = DeserializePayload(row.PayloadJson);
        return new NgrokEntity
        {
            Id             = row.Id,
            Name           = row.Name,
            Description    = row.Description,
            IsDefault      = row.IsDefault,
            IsActive       = row.IsActive,
            CreatedAtUtc   = row.CreatedAtUtc,
            UpdatedAtUtc   = row.UpdatedAtUtc,
            AuthToken      = p.AuthToken,
            ExecutablePath = p.ExecutablePath,
            AdditionalArgs = p.AdditionalArgs,
            BaseUrl        = p.BaseUrl
        };
    }

    private static string SerializePayload(NgrokEntity e) =>
        JsonSerializer.Serialize(new NgrokPayload
        {
            AuthToken      = e.AuthToken,
            ExecutablePath = e.ExecutablePath,
            AdditionalArgs = e.AdditionalArgs,
            BaseUrl        = e.BaseUrl
        });

    private static NgrokPayload DeserializePayload(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new NgrokPayload();
        return JsonSerializer.Deserialize<NgrokPayload>(json) ?? new NgrokPayload();
    }

    private sealed class NgrokPayload
    {
        public string AuthToken { get; set; } = string.Empty;
        public string ExecutablePath { get; set; } = string.Empty;
        public string AdditionalArgs { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "http://127.0.0.1:4040/";
    }
}
