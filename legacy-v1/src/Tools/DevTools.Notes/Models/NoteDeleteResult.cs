namespace DevTools.Notes.Models;

public sealed record NoteDeleteResult(
    string Key,
    string Path,
    bool Existed);
