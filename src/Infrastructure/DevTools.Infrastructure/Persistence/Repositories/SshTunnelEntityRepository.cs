using System.Text.Json;
using DevTools.Infrastructure.Persistence.Entities;
using DevTools.SSHTunnel.Models;
using DevTools.SSHTunnel.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DevTools.Infrastructure.Persistence.Repositories;

public sealed class SshTunnelEntityRepository : ISshTunnelEntityRepository
{
    private const string ToolSlug = "sshtunnel";
    private readonly DbContextOptions<DevToolsDbContext> _dbOptions;

    public SshTunnelEntityRepository(DbContextOptions<DevToolsDbContext> dbOptions)
    {
        _dbOptions = dbOptions;
    }

    public async Task<IReadOnlyList<SshTunnelEntity>> ListAsync(CancellationToken ct = default)
    {
        await using var db = new DevToolsDbContext(_dbOptions);
        var rows = await db.ToolConfigurations
            .Where(x => x.ToolSlug == ToolSlug)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Name)
            .ToListAsync(ct);
        return rows.Select(ToModel).ToList();
    }

    public async Task<SshTunnelEntity?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        await using var db = new DevToolsDbContext(_dbOptions);
        var row = await db.ToolConfigurations
            .SingleOrDefaultAsync(x => x.ToolSlug == ToolSlug && x.Id == id, ct);
        return row is null ? null : ToModel(row);
    }

    public async Task<SshTunnelEntity?> GetDefaultAsync(CancellationToken ct = default)
    {
        await using var db = new DevToolsDbContext(_dbOptions);
        var row = await db.ToolConfigurations
            .Where(x => x.ToolSlug == ToolSlug && x.IsDefault)
            .FirstOrDefaultAsync(ct);
        return row is null ? null : ToModel(row);
    }

    public async Task UpsertAsync(SshTunnelEntity entity, CancellationToken ct = default)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Id))
            throw new InvalidOperationException("SshTunnelEntity.Id é obrigatório.");

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

    private static SshTunnelEntity ToModel(ToolConfigurationEntity row)
    {
        var p = DeserializePayload(row.PayloadJson);
        return new SshTunnelEntity
        {
            Id                    = row.Id,
            Name                  = row.Name,
            Description           = row.Description,
            IsDefault             = row.IsDefault,
            IsActive              = row.IsActive,
            CreatedAtUtc          = row.CreatedAtUtc,
            UpdatedAtUtc          = row.UpdatedAtUtc,
            SshHost               = p.SshHost,
            SshPort               = p.SshPort,
            SshUser               = p.SshUser,
            LocalBindHost         = p.LocalBindHost,
            LocalPort             = p.LocalPort,
            RemoteHost            = p.RemoteHost,
            RemotePort            = p.RemotePort,
            IdentityFile          = p.IdentityFile,
            StrictHostKeyChecking = p.StrictHostKeyChecking,
            ConnectTimeoutSeconds = p.ConnectTimeoutSeconds
        };
    }

    private static string SerializePayload(SshTunnelEntity e) =>
        JsonSerializer.Serialize(new SshTunnelPayload
        {
            SshHost               = e.SshHost,
            SshPort               = e.SshPort,
            SshUser               = e.SshUser,
            LocalBindHost         = e.LocalBindHost,
            LocalPort             = e.LocalPort,
            RemoteHost            = e.RemoteHost,
            RemotePort            = e.RemotePort,
            IdentityFile          = e.IdentityFile,
            StrictHostKeyChecking = e.StrictHostKeyChecking,
            ConnectTimeoutSeconds = e.ConnectTimeoutSeconds
        });

    private static SshTunnelPayload DeserializePayload(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new SshTunnelPayload();
        return JsonSerializer.Deserialize<SshTunnelPayload>(json) ?? new SshTunnelPayload();
    }

    private sealed class SshTunnelPayload
    {
        public string SshHost { get; set; } = string.Empty;
        public int SshPort { get; set; } = 22;
        public string SshUser { get; set; } = string.Empty;
        public string LocalBindHost { get; set; } = "127.0.0.1";
        public int LocalPort { get; set; } = 14331;
        public string RemoteHost { get; set; } = "127.0.0.1";
        public int RemotePort { get; set; } = 1433;
        public string? IdentityFile { get; set; }
        public SshStrictHostKeyChecking StrictHostKeyChecking { get; set; } = SshStrictHostKeyChecking.Default;
        public int? ConnectTimeoutSeconds { get; set; }
    }
}
