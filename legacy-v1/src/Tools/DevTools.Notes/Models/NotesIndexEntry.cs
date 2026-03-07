namespace DevTools.Notes.Models;

public sealed class NotesIndexEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
    public string Sha256 { get; set; } = string.Empty;
    public List<string>? Tags { get; set; }
}
