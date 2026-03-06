using System;

namespace DevTools.Presentation.Wpf.Persistence.Entities;

public sealed class ToolConfigurationEntity
{
    public long Id { get; set; }
    public string ToolKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public string OptionsJson { get; set; } = "{}";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}


