using System.Text.Json;
using DevTools.Core.Abstractions;
using DevTools.Core.Models;
using DevTools.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevTools.Infrastructure.Persistence.Repositories;

public sealed class ToolUsageHistoryRepository : IToolUsageHistoryRepository
{
    private const string SettingKeyPrefix = "tool_history:";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly DbContextOptions<DevToolsDbContext> _dbOptions;

    public ToolUsageHistoryRepository(DbContextOptions<DevToolsDbContext> dbOptions)
    {
        _dbOptions = dbOptions;
    }

    public async Task<IReadOnlyList<ToolUsageHistoryEntry>> ListAsync(string toolSlug, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(toolSlug))
            return [];

        await using var db = new DevToolsDbContext(_dbOptions);
        var key = BuildKey(toolSlug);
        var row = await db.AppSettings.SingleOrDefaultAsync(x => x.Key == key, ct);
        if (row is null || string.IsNullOrWhiteSpace(row.ValueJson))
            return [];

        return Deserialize(row.ValueJson)
            .OrderByDescending(x => x.UsedAtUtc)
            .ToArray();
    }

    public async Task AddAsync(string toolSlug, ToolUsageHistoryEntry entry, int maxItems = 10, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(toolSlug))
            return;

        if (entry is null)
            throw new ArgumentNullException(nameof(entry));

        maxItems = Math.Max(1, maxItems);

        await using var db = new DevToolsDbContext(_dbOptions);
        var key = BuildKey(toolSlug);
        var row = await db.AppSettings.SingleOrDefaultAsync(x => x.Key == key, ct);

        var list = row is null
            ? new List<ToolUsageHistoryEntry>()
            : Deserialize(row.ValueJson).ToList();

        list.Add(entry);
        var trimmed = list
            .OrderBy(x => x.UsedAtUtc)
            .TakeLast(maxItems)
            .ToList();

        var json = JsonSerializer.Serialize(trimmed, JsonOptions);

        if (row is null)
        {
            db.AppSettings.Add(new AppSettingEntity
            {
                Key = key,
                ValueJson = json,
                UpdatedAtUtc = DateTime.UtcNow
            });
        }
        else
        {
            row.ValueJson = json;
            row.UpdatedAtUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
    }

    private static string BuildKey(string toolSlug)
        => $"{SettingKeyPrefix}{toolSlug.Trim().ToLowerInvariant()}";

    private static IReadOnlyList<ToolUsageHistoryEntry> Deserialize(string json)
    {
        try
        {
            var list = JsonSerializer.Deserialize<List<ToolUsageHistoryEntry>>(json, JsonOptions);
            return list ?? [];
        }
        catch
        {
            return [];
        }
    }
}
