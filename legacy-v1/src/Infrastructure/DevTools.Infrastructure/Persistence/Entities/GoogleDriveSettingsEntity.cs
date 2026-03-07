using System;

namespace DevTools.Infrastructure.Persistence.Entities;

public sealed class GoogleDriveSettingsEntity
{
    public int Id { get; set; } = 1;
    public bool IsEnabled { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public byte[]? ClientSecretProtected { get; set; }
    public string FolderName { get; set; } = "DevToolsNotes";
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}


