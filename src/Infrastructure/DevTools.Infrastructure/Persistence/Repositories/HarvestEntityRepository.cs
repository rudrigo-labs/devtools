using System.Text.Json;
using DevTools.Harvest.Models;
using DevTools.Harvest.Repositories;
using DevTools.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevTools.Infrastructure.Persistence.Repositories;

public sealed class HarvestEntityRepository : IHarvestEntityRepository
{
    private const string ToolSlug = "harvest";
    private readonly DbContextOptions<DevToolsDbContext> _dbOptions;

    public HarvestEntityRepository(DbContextOptions<DevToolsDbContext> dbOptions)
    {
        _dbOptions = dbOptions;
    }

    public async Task<IReadOnlyList<HarvestEntity>> ListAsync(CancellationToken ct = default)
    {
        await using var db = new DevToolsDbContext(_dbOptions);
        var rows = await db.ToolConfigurations
            .Where(x => x.ToolSlug == ToolSlug)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Name)
            .ToListAsync(ct);

        return rows.Select(ToModel).ToList();
    }

    public async Task<HarvestEntity?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;

        await using var db = new DevToolsDbContext(_dbOptions);
        var row = await db.ToolConfigurations
            .SingleOrDefaultAsync(x => x.ToolSlug == ToolSlug && x.Id == id, ct);

        return row is null ? null : ToModel(row);
    }

    public async Task UpsertAsync(HarvestEntity entity, CancellationToken ct = default)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Id))
            throw new InvalidOperationException("HarvestEntity.Id é obrigatório para persistência.");

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
        if (string.IsNullOrWhiteSpace(id)) return;

        await using var db = new DevToolsDbContext(_dbOptions);
        var row = await db.ToolConfigurations
            .SingleOrDefaultAsync(x => x.ToolSlug == ToolSlug && x.Id == id, ct);

        if (row is null) return;

        db.ToolConfigurations.Remove(row);
        await db.SaveChangesAsync(ct);
    }

    public async Task<HarvestEntity?> GetDefaultAsync(CancellationToken ct = default)
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
            throw new ArgumentException("Id é obrigatório.", nameof(id));

        await using var db = new DevToolsDbContext(_dbOptions);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var target = await db.ToolConfigurations
            .SingleOrDefaultAsync(x => x.ToolSlug == ToolSlug && x.Id == id, ct)
            ?? throw new InvalidOperationException($"Configuração Harvest '{id}' não encontrada.");

        await db.ToolConfigurations
            .Where(x => x.ToolSlug == ToolSlug && x.IsDefault)
            .ExecuteUpdateAsync(x => x.SetProperty(e => e.IsDefault, false), ct);

        target.IsDefault = true;
        target.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    // ── Mapeamento ────────────────────────────────────────────────────────────

    private static HarvestEntity ToModel(ToolConfigurationEntity entity)
    {
        var p = DeserializePayload(entity.PayloadJson);
        return new HarvestEntity
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            IsActive = entity.IsActive,
            IsDefault = entity.IsDefault,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc,

            RootPath = p.RootPath,
            OutputPath = p.OutputPath,
            CopyFiles = p.CopyFiles,
            MinScore = p.MinScore,

            IgnoredDirectories = p.IgnoredDirectories.Length > 0
                ? p.IgnoredDirectories
                : HarvestDefaults.DefaultIgnoredDirectories,
            IgnoredExtensions = p.IgnoredExtensions.Length > 0
                ? p.IgnoredExtensions
                : HarvestDefaults.DefaultIgnoredExtensions,
            IncludedExtensions = p.IncludedExtensions.Length > 0
                ? p.IncludedExtensions
                : HarvestDefaults.DefaultIncludedExtensions,
            MaxFileSizeKb = p.MaxFileSizeKb is > 0 ? p.MaxFileSizeKb : null,

            FanInWeight = p.FanInWeight,
            FanOutWeight = p.FanOutWeight,
            KeywordDensityWeight = p.KeywordDensityWeight,
            DensityScale = p.DensityScale > 0 ? p.DensityScale : HarvestDefaults.DefaultDensityScale,
            StaticMethodThreshold = p.StaticMethodThreshold,
            StaticMethodBonus = p.StaticMethodBonus,
            DeadCodePenalty = p.DeadCodePenalty,
            LargeFileThresholdLines = p.LargeFileThresholdLines,
            LargeFilePenalty = p.LargeFilePenalty,

            Categories = p.Categories?.Count > 0
                ? p.Categories
                : HarvestDefaults.DefaultCategories()
        };
    }

    private static string SerializePayload(HarvestEntity model)
    {
        var payload = new HarvestPayload
        {
            RootPath = model.RootPath,
            OutputPath = model.OutputPath,
            CopyFiles = model.CopyFiles,
            MinScore = model.MinScore,

            IgnoredDirectories = model.IgnoredDirectories?.ToArray() ?? [],
            IgnoredExtensions = model.IgnoredExtensions?.ToArray() ?? [],
            IncludedExtensions = model.IncludedExtensions?.ToArray() ?? [],
            MaxFileSizeKb = model.MaxFileSizeKb,

            FanInWeight = model.FanInWeight,
            FanOutWeight = model.FanOutWeight,
            KeywordDensityWeight = model.KeywordDensityWeight,
            DensityScale = model.DensityScale,
            StaticMethodThreshold = model.StaticMethodThreshold,
            StaticMethodBonus = model.StaticMethodBonus,
            DeadCodePenalty = model.DeadCodePenalty,
            LargeFileThresholdLines = model.LargeFileThresholdLines,
            LargeFilePenalty = model.LargeFilePenalty,

            Categories = model.Categories
        };

        return JsonSerializer.Serialize(payload);
    }

    private static HarvestPayload DeserializePayload(string payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
            return new HarvestPayload();

        return JsonSerializer.Deserialize<HarvestPayload>(payloadJson) ?? new HarvestPayload();
    }

    private sealed class HarvestPayload
    {
        public string RootPath { get; set; } = string.Empty;
        public string OutputPath { get; set; } = string.Empty;
        public bool CopyFiles { get; set; } = true;
        public int MinScore { get; set; }

        public string[] IgnoredDirectories { get; set; } = [];
        public string[] IgnoredExtensions { get; set; } = [];
        public string[] IncludedExtensions { get; set; } = [];
        public int? MaxFileSizeKb { get; set; }

        public double FanInWeight { get; set; } = HarvestDefaults.DefaultFanInWeight;
        public double FanOutWeight { get; set; } = HarvestDefaults.DefaultFanOutWeight;
        public double KeywordDensityWeight { get; set; } = HarvestDefaults.DefaultKeywordDensityWeight;
        public int DensityScale { get; set; } = HarvestDefaults.DefaultDensityScale;
        public int StaticMethodThreshold { get; set; } = HarvestDefaults.DefaultStaticMethodThreshold;
        public double StaticMethodBonus { get; set; } = HarvestDefaults.DefaultStaticMethodBonus;
        public double DeadCodePenalty { get; set; } = HarvestDefaults.DefaultDeadCodePenalty;
        public int LargeFileThresholdLines { get; set; } = HarvestDefaults.DefaultLargeFileThresholdLines;
        public double LargeFilePenalty { get; set; } = HarvestDefaults.DefaultLargeFilePenalty;

        public List<HarvestKeywordCategory>? Categories { get; set; }
    }
}
