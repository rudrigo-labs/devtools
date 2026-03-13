namespace DevTools.Notes.Models;

public sealed record NoteReadResult(
    string Key,
    string Path,
    string? Content,
    bool Exists);
