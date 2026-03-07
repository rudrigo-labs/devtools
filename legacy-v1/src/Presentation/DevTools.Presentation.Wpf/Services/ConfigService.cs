using System;
using System.Collections.Generic;
using DevTools.Infrastructure.Persistence;
using DevTools.Infrastructure.Persistence.Stores;
using DevTools.SSHTunnel.Models;

namespace DevTools.Presentation.Wpf.Services;

public class SshConfigSection
{
    public List<TunnelConfiguration> Configurations { get; set; } = new();
}

public class ConfigService
{
    private readonly ISettingsStore _store;

    public ConfigService()
    {
        var backend = StorageBackendResolver.Resolve();
        var jsonConfigPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        _store = SettingsStoreFactory.Create(backend, jsonConfigPath);
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
