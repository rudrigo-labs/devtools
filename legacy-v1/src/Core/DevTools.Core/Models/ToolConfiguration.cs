using System;
using System.Collections.Generic;

namespace DevTools.Core.Models;

public class ToolConfiguration
{
    public string ToolSlug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public Dictionary<string, string> Options { get; set; } = new();
    public bool IsDefault { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; }
}

public class ToolConfigurationContainer
{
    public List<ToolConfiguration> Configurations { get; set; } = new();
}


