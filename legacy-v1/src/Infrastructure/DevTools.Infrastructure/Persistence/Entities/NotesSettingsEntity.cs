using System;

namespace DevTools.Infrastructure.Persistence.Entities;

public sealed class NotesSettingsEntity
{
    public int Id { get; set; } = 1;
    public string StoragePath { get; set; } = string.Empty;
    public string DefaultFormat { get; set; } = ".txt";
    public bool AutoCloudSync { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}


