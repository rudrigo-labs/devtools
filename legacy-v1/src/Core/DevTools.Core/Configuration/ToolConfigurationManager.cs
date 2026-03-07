using System;
using System.Collections.Generic;
using System.Linq;
using DevTools.Core.Models;

namespace DevTools.Core.Configuration;

public class ToolConfigurationManager
{
    private readonly IToolConfigurationRepository _repository;

    public ToolConfigurationManager()
        : this(new ToolConfigurationRepository(new JsonFileToolConfigurationStore()))
    {
    }

    public ToolConfigurationManager(IToolConfigurationStore store)
        : this(new ToolConfigurationRepository(store))
    {
    }

    public ToolConfigurationManager(IToolConfigurationRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public List<ToolConfiguration> LoadConfigurations(string toolName)
    {
        return _repository.LoadConfigurations(toolName);
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
        _repository.SaveConfigurations(toolName, configurations);
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

}

