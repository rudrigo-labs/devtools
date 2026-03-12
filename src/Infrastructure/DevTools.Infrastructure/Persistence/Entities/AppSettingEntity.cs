namespace DevTools.Infrastructure.Persistence.Entities;

public sealed class AppSettingEntity
{
    public string Key { get; set; } = string.Empty;
    public string ValueJson { get; set; } = "{}";
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

