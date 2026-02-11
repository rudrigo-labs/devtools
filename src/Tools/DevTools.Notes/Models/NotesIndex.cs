namespace DevTools.Notes.Models;

public sealed class NotesIndex
{
    public int Version { get; set; } = 1;
    public List<NotesIndexEntry> Items { get; set; } = new();
}
