using System;
using System.Collections.Generic;
using System.Globalization;

namespace DevTools.Core.Models;

public sealed class NamedToolConfiguration
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string ToolSlug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public Dictionary<string, string> Payload { get; set; } = new();
}

public static class NamedToolConfigurationMapper
{
    public static NamedToolConfiguration FromToolConfiguration(string toolSlug, ToolConfiguration configuration)
    {
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        var payload = configuration.Options ?? new Dictionary<string, string>();
        var tool = string.IsNullOrWhiteSpace(configuration.ToolSlug) ? toolSlug : configuration.ToolSlug;

        return new NamedToolConfiguration
        {
            ToolSlug = tool,
            Name = configuration.Name,
            Description = configuration.Description,
            IsActive = configuration.IsActive,
            IsDefault = configuration.IsDefault,
            CreatedAtUtc = configuration.CreatedUtc == default ? DateTime.UtcNow : configuration.CreatedUtc,
            UpdatedAtUtc = configuration.UpdatedUtc == default ? DateTime.UtcNow : configuration.UpdatedUtc,
            Payload = new Dictionary<string, string>(payload)
        };
    }

    public static ToolConfiguration ToToolConfiguration(NamedToolConfiguration configuration)
    {
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        return new ToolConfiguration
        {
            ToolSlug = configuration.ToolSlug,
            Name = configuration.Name,
            Description = configuration.Description,
            IsActive = configuration.IsActive,
            IsDefault = configuration.IsDefault,
            CreatedUtc = configuration.CreatedAtUtc == default ? DateTime.UtcNow : configuration.CreatedAtUtc,
            UpdatedUtc = configuration.UpdatedAtUtc == default ? DateTime.UtcNow : configuration.UpdatedAtUtc,
            Options = configuration.Payload ?? new Dictionary<string, string>()
        };
    }
}

public static class ToolConfigurationMetadata
{
    private const string ToolSlugKey = "__meta:tool-slug";
    private const string DescriptionKey = "__meta:description";
    private const string IsActiveKey = "__meta:is-active";
    private const string CreatedUtcKey = "__meta:created-utc";

    public static void WriteToOptions(ToolConfiguration configuration)
    {
        configuration.Options ??= new Dictionary<string, string>();

        if (!string.IsNullOrWhiteSpace(configuration.ToolSlug))
        {
            configuration.Options[ToolSlugKey] = configuration.ToolSlug;
        }

        if (!string.IsNullOrWhiteSpace(configuration.Description))
        {
            configuration.Options[DescriptionKey] = configuration.Description;
        }

        configuration.Options[IsActiveKey] = configuration.IsActive ? "true" : "false";
        configuration.Options[CreatedUtcKey] = (configuration.CreatedUtc == default ? DateTime.UtcNow : configuration.CreatedUtc)
            .ToString("O", CultureInfo.InvariantCulture);
    }

    public static void ReadFromOptions(ToolConfiguration configuration)
    {
        configuration.Options ??= new Dictionary<string, string>();

        if (configuration.Options.TryGetValue(ToolSlugKey, out var toolSlug) && !string.IsNullOrWhiteSpace(toolSlug))
        {
            configuration.ToolSlug = toolSlug;
        }

        if (configuration.Options.TryGetValue(DescriptionKey, out var description))
        {
            configuration.Description = description ?? string.Empty;
        }

        if (configuration.Options.TryGetValue(IsActiveKey, out var isActiveRaw) && bool.TryParse(isActiveRaw, out var isActive))
        {
            configuration.IsActive = isActive;
        }

        if (configuration.Options.TryGetValue(CreatedUtcKey, out var createdRaw) &&
            DateTime.TryParse(createdRaw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var createdUtc))
        {
            configuration.CreatedUtc = createdUtc;
        }
        else if (configuration.CreatedUtc == default)
        {
            configuration.CreatedUtc = DateTime.UtcNow;
        }
    }
}


