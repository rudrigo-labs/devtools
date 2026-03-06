using System;
using System.Collections.Generic;
using System.Linq;
using DevTools.Core.Models;

namespace DevTools.Core.Configuration;

public class ToolConfigurationManager
{
    private readonly IToolConfigurationStore _store;

    public ToolConfigurationManager()
        : this(new JsonFileToolConfigurationStore())
    {
    }

    public ToolConfigurationManager(IToolConfigurationStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public List<ToolConfiguration> LoadConfigurations(string toolName)
    {
        var configurations = _store.LoadConfigurations(toolName);
        foreach (var configuration in configurations)
        {
            NormalizeConfiguration(configuration, toolName);
        }

        return configurations;
    }

    public void SaveConfiguration(string toolName, ToolConfiguration configuration)
    {
        var configurations = LoadConfigurations(toolName);
        var existing = configurations.FirstOrDefault(p => p.Name.Equals(configuration.Name, StringComparison.OrdinalIgnoreCase));
        
        if (existing != null)
        {
            configurations.Remove(existing);
        }
        
        // Update timestamp
        configuration.UpdatedUtc = DateTime.UtcNow;
        configurations.Add(configuration);
        
        SaveConfigurationsInternal(toolName, configurations);
    }

    public void SaveConfigurations(string toolName, List<ToolConfiguration> configurations)
    {
        SaveConfigurationsInternal(toolName, configurations);
    }

    public void DeleteConfiguration(string toolName, string configurationName)
    {
        var configurations = LoadConfigurations(toolName);
        var existing = configurations.FirstOrDefault(p => p.Name.Equals(configurationName, StringComparison.OrdinalIgnoreCase));
        
        if (existing != null)
        {
            configurations.Remove(existing);
            SaveConfigurationsInternal(toolName, configurations);
        }
    }
    
    public ToolConfiguration? GetConfiguration(string toolName, string configurationName)
    {
        var configurations = LoadConfigurations(toolName);
        return configurations.FirstOrDefault(p => p.Name.Equals(configurationName, StringComparison.OrdinalIgnoreCase));
    }

    public ToolConfiguration? GetDefaultConfiguration(string toolName)
    {
        var configurations = LoadConfigurations(toolName);
        return configurations.FirstOrDefault(p => p.IsDefault);
    }

    private void SaveConfigurationsInternal(string toolName, List<ToolConfiguration> configurations)
    {
        foreach (var configuration in configurations)
        {
            NormalizeConfiguration(configuration, toolName);
            ToolConfigurationMetadata.WriteToOptions(configuration);
        }

        _store.SaveConfigurations(toolName, configurations);
    }

    public List<NamedToolConfiguration> LoadNamedConfigurations(string toolName)
    {
        return LoadConfigurations(toolName)
            .Select(p => NamedToolConfigurationMapper.FromToolConfiguration(toolName, p))
            .ToList();
    }

    public void SaveNamedConfigurations(string toolName, List<NamedToolConfiguration> configurations)
    {
        var toolConfigurations = configurations
            .Select(NamedToolConfigurationMapper.ToToolConfiguration)
            .ToList();

        SaveConfigurationsInternal(toolName, toolConfigurations);
    }

    private static void NormalizeConfiguration(ToolConfiguration configuration, string toolName)
    {
        if (configuration == null)
        {
            return;
        }

        ToolConfigurationMetadata.ReadFromOptions(configuration);

        if (string.IsNullOrWhiteSpace(configuration.ToolSlug))
        {
            configuration.ToolSlug = toolName;
        }

        if (configuration.CreatedUtc == default)
        {
            configuration.CreatedUtc = DateTime.UtcNow;
        }

        if (configuration.UpdatedUtc == default)
        {
            configuration.UpdatedUtc = DateTime.UtcNow;
        }
    }
}

