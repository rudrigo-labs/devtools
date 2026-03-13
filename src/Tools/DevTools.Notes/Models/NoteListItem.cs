namespace DevTools.Notes.Models;

public sealed record NoteListItem(
    string Id,
    string Title,
    string FileName,
    DateTime CreatedUtc,
    DateTime UpdatedUtc,
    IReadOnlyList<string>? Tags = null);
