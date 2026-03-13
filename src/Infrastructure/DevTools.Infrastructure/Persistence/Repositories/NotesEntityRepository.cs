using System.Text.Json;
using DevTools.Notes.Models;
using DevTools.Notes.Repositories;
using DevTools.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevTools.Infrastructure.Persistence.Repositories;

public sealed class NotesEntityRepository : INotesEntityRepository
{
    private const string ToolSlug = "notes";
    private readonly DbContextOptions<DevToolsDbContext> _dbOptions;

    public NotesEntityRepository(DbContextOptions<DevToolsDbContext> dbOptions)
    {
        _dbOptions = dbOptions;
    }

    public async Task<IReadOnlyList<NotesEntity>> ListAsync(CancellationToken ct = default)
    {
        await using var db = new DevToolsDbContext(_dbOptions);
        var rows = await db.ToolConfigurations
            .Where(x => x.ToolSlug == ToolSlug)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Name)
            .ToListAsync(ct);

        return rows.Select(ToModel).ToList();
    }

    public async Task<NotesEntity?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;

        await using var db = new DevToolsDbContext(_dbOptions);
        var row = await db.ToolConfigurations
            .SingleOrDefaultAsync(x => x.ToolSlug == ToolSlug && x.Id == id, ct);

        return row is null ? null : ToModel(row);
    }

    public async Task<NotesEntity?> GetDefaultAsync(CancellationToken ct = default)
    {
        await using var db = new DevToolsDbContext(_dbOptions);
        var row = await db.ToolConfigurations
            .Where(x => x.ToolSlug == ToolSlug && x.IsDefault)
            .FirstOrDefaultAsync(ct);

        return row is null ? null : ToModel(row);
    }

    public async Task UpsertAsync(NotesEntity entity, CancellationToken ct = default)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Id))
            throw new InvalidOperationException("NotesEntity.Id é obrigatório para persistência.");

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

    private static NotesEntity ToModel(ToolConfigurationEntity row)
    {
        var p = DeserializePayload(row.PayloadJson);
        return new NotesEntity
        {
            Id                          = row.Id,
            Name                        = row.Name,
            Description                 = row.Description,
            IsDefault                   = row.IsDefault,
            IsActive                    = row.IsActive,
            CreatedAtUtc                = row.CreatedAtUtc,
            UpdatedAtUtc                = row.UpdatedAtUtc,
            LocalRootPath               = p.LocalRootPath,
            DefaultExtension            = p.DefaultExtension,
            GoogleDriveEnabled          = p.GoogleDriveEnabled,
            GoogleDriveCredentialsPath  = p.GoogleDriveCredentialsPath,
            GoogleDriveFolderId         = p.GoogleDriveFolderId,
            OAuthTokenCachePath         = p.OAuthTokenCachePath
        };
    }

    private static string SerializePayload(NotesEntity model)
    {
        var payload = new NotesPayload
        {
            LocalRootPath              = model.LocalRootPath,
            DefaultExtension           = model.DefaultExtension,
            GoogleDriveEnabled         = model.GoogleDriveEnabled,
            GoogleDriveCredentialsPath = model.GoogleDriveCredentialsPath,
            GoogleDriveFolderId        = model.GoogleDriveFolderId,
            OAuthTokenCachePath        = model.OAuthTokenCachePath
        };
        return JsonSerializer.Serialize(payload);
    }

    private static NotesPayload DeserializePayload(string payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
            return new NotesPayload();

        return JsonSerializer.Deserialize<NotesPayload>(payloadJson) ?? new NotesPayload();
    }

    private sealed class NotesPayload
    {
        public string LocalRootPath              { get; set; } = string.Empty;
        public string DefaultExtension           { get; set; } = ".md";
        public bool   GoogleDriveEnabled         { get; set; }
        public string GoogleDriveCredentialsPath { get; set; } = string.Empty;
        public string GoogleDriveFolderId        { get; set; } = string.Empty;
        public string OAuthTokenCachePath        { get; set; } = string.Empty;
    }
}
