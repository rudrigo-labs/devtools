namespace DevTools.Notes.Models;

public sealed record NoteCreateResult(
    string Id,
    string Title,
    string FileName,
    string Path,
    string Sha256,
    DateTime CreatedUtc,
    DateTime UpdatedUtc);
