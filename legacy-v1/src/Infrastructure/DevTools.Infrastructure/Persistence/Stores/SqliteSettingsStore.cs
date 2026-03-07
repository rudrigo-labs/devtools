using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using DevTools.Harvest.Configuration;
using DevTools.Infrastructure.Persistence.Entities;
using DevTools.Infrastructure.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace DevTools.Infrastructure.Persistence.Stores;

public sealed class SqliteSettingsStore : ISettingsStore
{
    private readonly DbContextOptions<DevToolsDbContext> _dbOptions;
    private readonly string _databasePath;

    public SqliteSettingsStore(DbContextOptions<DevToolsDbContext> dbOptions, string databasePath)
    {
        _dbOptions = dbOptions;
        _databasePath = databasePath;
    }

    public string Location => _databasePath;

    public bool IsConfigured()
    {
        try
        {
            using var db = new DevToolsDbContext(_dbOptions);
            return db.Database.CanConnect();
        }
        catch (Exception ex)
        {
            InfraLogger.Error("Failed to check SQLite settings store connectivity.", ex);
            return false;
        }
    }

    public T GetSection<T>(string sectionName) where T : new()
    {
        try
        {
            using var db = new DevToolsDbContext(_dbOptions);
            var row = db.AppSettings.SingleOrDefault(x => x.Key == sectionName);
            if (row == null || string.IsNullOrWhiteSpace(row.Value))
            {
                return new T();
            }

            return JsonSerializer.Deserialize<T>(
                       row.Value,
                       new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                   ?? new T();
        }
        catch (Exception ex)
        {
            InfraLogger.Error($"Failed to read SQLite settings section '{sectionName}'.", ex);
            return new T();
        }
    }

    public void SaveSection<T>(string sectionName, T data)
    {
        try
        {
            using var db = new DevToolsDbContext(_dbOptions);
            var serialized = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            var row = db.AppSettings.SingleOrDefault(x => x.Key == sectionName);
            if (row == null)
            {
                row = new AppSettingEntity
                {
                    Key = sectionName,
                    Value = serialized,
                    UpdatedAtUtc = DateTime.UtcNow
                };
                db.AppSettings.Add(row);
            }
            else
            {
                row.Value = serialized;
                row.UpdatedAtUtc = DateTime.UtcNow;
            }

            db.SaveChanges();
        }
        catch (Exception ex)
        {
            InfraLogger.Error($"Failed to save SQLite settings section '{sectionName}'.", ex);
        }
    }

    public void CreateDefaultIfNotExists()
    {
        try
        {
            using var db = new DevToolsDbContext(_dbOptions);
            if (db.AppSettings.Any())
            {
                return;
            }

            SaveSection("ConnectionStrings", new { });
            SaveSection("Email", new
            {
                SmtpHost = "",
                SmtpPort = 587,
                Username = "",
                Password = ""
            });

            SaveSection("Ssh", new
            {
                Configurations = new[]
                {
                    new
                    {
                        Name = "default",
                        SshHost = "",
                        SshPort = 22,
                        SshUser = "",
                        LocalBindHost = "127.0.0.1",
                        LocalPort = 14331,
                        RemoteHost = "127.0.0.1",
                        RemotePort = 1433,
                        StrictHostKeyChecking = "Default",
                        ConnectTimeoutSeconds = (int?)null
                    }
                }
            });

            SaveSection("Harvest", new HarvestConfig
            {
                MinScoreDefault = 0,
                TopDefault = 100,
                Weights = new HarvestWeights
                {
                    FanInWeight = 2.0,
                    FanOutWeight = 0.5,
                    KeywordDensityWeight = 1.0,
                    DeadCodePenalty = 5.0
                },
                Rules = new HarvestRules
                {
                    Extensions = new List<string> { ".cs", ".xml", ".json", ".xaml" },
                    ExcludeDirectories = HarvestDefaults.DefaultExcludeDirectories.ToList()
                }
            });
        }
        catch (Exception ex)
        {
            InfraLogger.Error("Failed to create default SQLite settings sections.", ex);
        }
    }
}

