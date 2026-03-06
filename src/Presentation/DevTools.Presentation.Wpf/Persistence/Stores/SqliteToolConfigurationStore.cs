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

public sealed class SqliteToolConfigurationStore : IToolConfigurationStore
{
    private readonly DbContextOptions<DevToolsDbContext> _dbOptions;

    public SqliteToolConfigurationStore(DbContextOptions<DevToolsDbContext> dbOptions)
    {
        _dbOptions = dbOptions;
    }

    public List<ToolConfiguration> LoadConfigurations(string toolName)
    {
        if (string.IsNullOrWhiteSpace(toolName))
        {
            return new List<ToolConfiguration>();
        }

        try
        {
            using var db = new DevToolsDbContext(_dbOptions);
            var rows = db.ToolConfigurations
                .Where(x => x.ToolKey == toolName)
                .OrderByDescending(x => x.IsDefault)
                .ThenBy(x => x.Name)
                .ToList();

            return rows.Select(ToModel).ToList();
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Failed to load configurations for tool '{toolName}' from SQLite.", ex);
            return new List<ToolConfiguration>();
        }
    }

    public void SaveConfigurations(string toolName, List<ToolConfiguration> configurations)
    {
        if (string.IsNullOrWhiteSpace(toolName))
        {
            return;
        }

        try
        {
            using var db = new DevToolsDbContext(_dbOptions);
            using var tx = db.Database.BeginTransaction();

            var existing = db.ToolConfigurations.Where(x => x.ToolKey == toolName).ToList();
            if (existing.Count > 0)
            {
                db.ToolConfigurations.RemoveRange(existing);
                db.SaveChanges();
            }

            foreach (var configuration in configurations)
            {
                var now = DateTime.UtcNow;
                ToolConfigurationMetadata.WriteToOptions(configuration);
                var entity = new ToolConfigurationEntity
                {
                    ToolKey = toolName,
                    Name = configuration.Name,
                    IsDefault = configuration.IsDefault,
                    OptionsJson = JsonSerializer.Serialize(configuration.Options ?? new Dictionary<string, string>()),
                    CreatedAtUtc = configuration.CreatedUtc == default ? now : configuration.CreatedUtc,
                    UpdatedAtUtc = configuration.UpdatedUtc == default ? now : configuration.UpdatedUtc
                };
                db.ToolConfigurations.Add(entity);
            }

            db.SaveChanges();
            tx.Commit();
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Failed to save configurations for tool '{toolName}' into SQLite.", ex);
        }
    }

    private static ToolConfiguration ToModel(ToolConfigurationEntity entity)
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

        var configuration = new ToolConfiguration
        {
            ToolSlug = entity.ToolKey,
            Name = entity.Name,
            IsDefault = entity.IsDefault,
            Options = options,
            CreatedUtc = entity.CreatedAtUtc,
            UpdatedUtc = entity.UpdatedAtUtc
        };

        ToolConfigurationMetadata.ReadFromOptions(configuration);
        return configuration;
    }
}



