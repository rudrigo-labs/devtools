using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using DevTools.Core.Configuration;
using DevTools.Core.Models;
using DevTools.Presentation.Wpf.Persistence.Entities;
using DevTools.Presentation.Wpf.Services;
using Microsoft.EntityFrameworkCore;

namespace DevTools.Presentation.Wpf.Persistence.Stores;

public sealed class SqliteProfileStore : IProfileStore
{
    private readonly DbContextOptions<DevToolsDbContext> _dbOptions;

    public SqliteProfileStore(DbContextOptions<DevToolsDbContext> dbOptions)
    {
        _dbOptions = dbOptions;
    }

    public List<ToolProfile> LoadProfiles(string toolName)
    {
        if (string.IsNullOrWhiteSpace(toolName))
        {
            return new List<ToolProfile>();
        }

        try
        {
            using var db = new DevToolsDbContext(_dbOptions);
            var rows = db.ToolProfiles
                .Where(x => x.ToolKey == toolName)
                .OrderByDescending(x => x.IsDefault)
                .ThenBy(x => x.Name)
                .ToList();

            return rows.Select(ToModel).ToList();
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Failed to load profiles for tool '{toolName}' from SQLite.", ex);
            return new List<ToolProfile>();
        }
    }

    public void SaveProfiles(string toolName, List<ToolProfile> profiles)
    {
        if (string.IsNullOrWhiteSpace(toolName))
        {
            return;
        }

        try
        {
            using var db = new DevToolsDbContext(_dbOptions);
            using var tx = db.Database.BeginTransaction();

            var existing = db.ToolProfiles.Where(x => x.ToolKey == toolName).ToList();
            if (existing.Count > 0)
            {
                db.ToolProfiles.RemoveRange(existing);
                db.SaveChanges();
            }

            foreach (var profile in profiles)
            {
                var now = DateTime.UtcNow;
                var entity = new ToolProfileEntity
                {
                    ToolKey = toolName,
                    Name = profile.Name,
                    IsDefault = profile.IsDefault,
                    OptionsJson = JsonSerializer.Serialize(profile.Options ?? new Dictionary<string, string>()),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = profile.UpdatedUtc == default ? now : profile.UpdatedUtc
                };
                db.ToolProfiles.Add(entity);
            }

            db.SaveChanges();
            tx.Commit();
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Failed to save profiles for tool '{toolName}' into SQLite.", ex);
        }
    }

    private static ToolProfile ToModel(ToolProfileEntity entity)
    {
        Dictionary<string, string> options;
        try
        {
            options = JsonSerializer.Deserialize<Dictionary<string, string>>(entity.OptionsJson)
                      ?? new Dictionary<string, string>();
        }
        catch
        {
            options = new Dictionary<string, string>();
        }

        return new ToolProfile
        {
            Name = entity.Name,
            IsDefault = entity.IsDefault,
            Options = options,
            UpdatedUtc = entity.UpdatedAtUtc
        };
    }
}

