namespace DevTools.Notes.Entities;

public sealed class NoteMetadataEntity
{
    public string NoteKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Extension { get; set; } = ".txt";
    public DateTime LastLocalWriteUtc { get; set; }
    public DateTime? LastCloudSyncUtc { get; set; }
    public string? LastCloudStatus { get; set; }
    public string? Hash { get; set; }
}
