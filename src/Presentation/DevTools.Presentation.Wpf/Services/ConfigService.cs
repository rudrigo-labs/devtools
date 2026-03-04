using System;
using System.Collections.Generic;
using DevTools.Presentation.Wpf.Persistence;
using DevTools.Presentation.Wpf.Persistence.Stores;
using DevTools.SSHTunnel.Models;
using Microsoft.EntityFrameworkCore;

namespace DevTools.Presentation.Wpf.Services;

public class SshConfigSection
{
    public List<TunnelProfile> Profiles { get; set; } = new();
}

public class ConfigService
{
    private readonly ISettingsStore _store;

    public ConfigService()
    {
        var backend = StorageBackendResolver.Resolve();
        var jsonConfigPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

        if (backend == StorageBackend.Sqlite)
        {
            var pathProvider = new SqlitePathProvider();
            var dbPath = pathProvider.GetDatabasePath();
            var dbOptions = new DbContextOptionsBuilder<DevToolsDbContext>()
                .UseSqlite(pathProvider.GetConnectionString())
                .Options;

            _store = new SqliteSettingsStore(dbOptions, dbPath);
        }
        else
        {
            _store = new JsonSettingsStore(jsonConfigPath);
        }
    }

    public ConfigService(ISettingsStore store)
    {
        _store = store;
    }

    public string ConfigPath => _store.Location;

    public bool IsConfigured()
    {
        return _store.IsConfigured();
    }

    public T GetSection<T>(string sectionName) where T : new()
    {
        return _store.GetSection<T>(sectionName);
    }

    public void SaveSection<T>(string sectionName, T data)
    {
        _store.SaveSection(sectionName, data);
    }

    public void CreateDefaultIfNotExists()
    {
        _store.CreateDefaultIfNotExists();
    }
}
