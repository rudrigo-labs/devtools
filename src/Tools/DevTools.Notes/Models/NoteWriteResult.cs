namespace DevTools.Notes.Models;

public sealed record NoteWriteResult(
    string Key,
    string Path,
    long BytesWritten,
    bool Overwritten);
