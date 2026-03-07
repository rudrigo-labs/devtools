using System;
using System.Collections.Generic;
using DevTools.Core.Models;

namespace DevTools.Core.Configuration;

public sealed class ToolConfigurationRepository : IToolConfigurationRepository
{
    private readonly IToolConfigurationStore _store;

    public ToolConfigurationRepository(IToolConfigurationStore store)
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

    public void SaveConfigurations(string toolName, List<ToolConfiguration> configurations)
    {
        foreach (var configuration in configurations)
        {
            NormalizeConfiguration(configuration, toolName);
            ToolConfigurationMetadata.WriteToOptions(configuration);
        }

        _store.SaveConfigurations(toolName, configurations);
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
