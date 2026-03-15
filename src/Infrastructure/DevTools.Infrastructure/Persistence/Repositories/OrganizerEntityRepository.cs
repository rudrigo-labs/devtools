using System.Text.Json;
using DevTools.Infrastructure.Persistence.Entities;
using DevTools.Organizer.Models;
using DevTools.Organizer.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DevTools.Infrastructure.Persistence.Repositories;

public sealed class OrganizerEntityRepository : IOrganizerEntityRepository
{
    private const string ToolSlug = "organizer";
    private readonly DbContextOptions<DevToolsDbContext> _dbOptions;

    public OrganizerEntityRepository(DbContextOptions<DevToolsDbContext> dbOptions)
    {
        _dbOptions = dbOptions;
    }

    public async Task<IReadOnlyList<OrganizerEntity>> ListAsync(CancellationToken ct = default)
    {
        await using var db = new DevToolsDbContext(_dbOptions);
        var rows = await db.ToolConfigurations
            .Where(x => x.ToolSlug == ToolSlug)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Name)
            .ToListAsync(ct);

        return rows.Select(ToModel).ToList();
    }

    public async Task<OrganizerEntity?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        await using var db = new DevToolsDbContext(_dbOptions);
        var row = await db.ToolConfigurations
            .SingleOrDefaultAsync(x => x.ToolSlug == ToolSlug && x.Id == id, ct);

        return row is null ? null : ToModel(row);
    }

    public async Task<OrganizerEntity?> GetDefaultAsync(CancellationToken ct = default)
    {
        await using var db = new DevToolsDbContext(_dbOptions);
        var row = await db.ToolConfigurations
            .Where(x => x.ToolSlug == ToolSlug && x.IsDefault)
            .OrderBy(x => x.Name)
            .FirstOrDefaultAsync(ct);

        return row is null ? null : ToModel(row);
    }

    public async Task UpsertAsync(OrganizerEntity entity, CancellationToken ct = default)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Id))
            throw new InvalidOperationException("OrganizerEntity.Id é obrigatório para persistência.");

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

    public async Task SetDefaultAsync(string id, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Id é obrigatório.", nameof(id));

        await using var db = new DevToolsDbContext(_dbOptions);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var target = await db.ToolConfigurations
            .SingleOrDefaultAsync(x => x.ToolSlug == ToolSlug && x.Id == id, ct)
            ?? throw new InvalidOperationException($"Configuração Organizer '{id}' não encontrada.");

        await db.ToolConfigurations
            .Where(x => x.ToolSlug == ToolSlug && x.IsDefault)
            .ExecuteUpdateAsync(x => x.SetProperty(e => e.IsDefault, false), ct);

        target.IsDefault = true;
        target.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    private static OrganizerEntity ToModel(ToolConfigurationEntity row)
    {
        var payload = DeserializePayload(row.PayloadJson);
        return new OrganizerEntity
        {
            Id = row.Id,
            Name = row.Name,
            Description = row.Description,
            IsDefault = row.IsDefault,
            IsActive = row.IsActive,
            CreatedAtUtc = row.CreatedAtUtc,
            UpdatedAtUtc = row.UpdatedAtUtc,
            InboxPath = payload.InboxPath,
            OutputPath = payload.OutputPath,
            MinScore = payload.MinScore,
            Apply = payload.Apply,
            AllowedExtensions = payload.AllowedExtensions?.Length > 0
                ? payload.AllowedExtensions
                : OrganizerDefaults.DefaultAllowedExtensions(),
            FileNameWeight = payload.FileNameWeight > 0
                ? payload.FileNameWeight
                : OrganizerDefaults.DefaultFileNameWeight,
            DeduplicateByHash = payload.DeduplicateByHash,
            DeduplicateByName = payload.DeduplicateByName,
            DeduplicateFirstLines = payload.DeduplicateFirstLines,
            DuplicatesFolderName = string.IsNullOrWhiteSpace(payload.DuplicatesFolderName)
                ? OrganizerDefaults.DefaultDuplicatesFolderName
                : payload.DuplicatesFolderName,
            OthersFolderName = string.IsNullOrWhiteSpace(payload.OthersFolderName)
                ? OrganizerDefaults.DefaultOthersFolderName
                : payload.OthersFolderName,
            Categories = payload.Categories?.Count > 0
                ? payload.Categories
                : OrganizerDefaults.DefaultCategories()
        };
    }

    private static string SerializePayload(OrganizerEntity model)
    {
        var payload = new OrganizerPayload
        {
            InboxPath = model.InboxPath,
            OutputPath = model.OutputPath,
            MinScore = model.MinScore,
            Apply = model.Apply,
            AllowedExtensions = model.AllowedExtensions,
            FileNameWeight = model.FileNameWeight,
            DeduplicateByHash = model.DeduplicateByHash,
            DeduplicateByName = model.DeduplicateByName,
            DeduplicateFirstLines = model.DeduplicateFirstLines,
            DuplicatesFolderName = model.DuplicatesFolderName,
            OthersFolderName = model.OthersFolderName,
            Categories = model.Categories
        };
        return JsonSerializer.Serialize(payload);
    }

    private static OrganizerPayload DeserializePayload(string payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
            return new OrganizerPayload();

        return JsonSerializer.Deserialize<OrganizerPayload>(payloadJson) ?? new OrganizerPayload();
    }

    private sealed class OrganizerPayload
    {
        public string InboxPath { get; set; } = string.Empty;
        public string OutputPath { get; set; } = string.Empty;
        public int MinScore { get; set; } = 3;
        public bool Apply { get; set; }
        public string[] AllowedExtensions { get; set; } = OrganizerDefaults.DefaultAllowedExtensions();
        public double FileNameWeight { get; set; } = OrganizerDefaults.DefaultFileNameWeight;
        public bool DeduplicateByHash { get; set; } = OrganizerDefaults.DefaultDeduplicateByHash;
        public bool DeduplicateByName { get; set; } = OrganizerDefaults.DefaultDeduplicateByName;
        public int DeduplicateFirstLines { get; set; } = OrganizerDefaults.DefaultDeduplicateFirstLines;
        public string DuplicatesFolderName { get; set; } = OrganizerDefaults.DefaultDuplicatesFolderName;
        public string OthersFolderName { get; set; } = OrganizerDefaults.DefaultOthersFolderName;
        public List<OrganizerCategory> Categories { get; set; } = OrganizerDefaults.DefaultCategories();
    }
}
