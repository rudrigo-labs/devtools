using System;

namespace DevTools.Notes.Cloud;

public class CloudFileMetadata
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime? ModifiedTime { get; set; }
    public long? Size { get; set; }
    public string? ContentHash { get; set; }
}
