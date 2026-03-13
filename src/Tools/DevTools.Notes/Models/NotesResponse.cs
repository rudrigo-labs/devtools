namespace DevTools.Notes.Models;

public sealed class NotesResponse
{
    public NotesAction Action { get; init; }
    public NoteReadResult? ReadResult { get; init; }
    public NoteWriteResult? WriteResult { get; init; }
    public NoteCreateResult? CreateResult { get; init; }
    public NoteListResult? ListResult { get; init; }
    public NoteDeleteResult? DeleteResult { get; init; }
    public NotesBackupReport? BackupReport { get; init; }
    public string? ExportedZipPath { get; init; }
    public bool DriveSkipped { get; init; }
    public string? DriveSkipReason { get; init; }

    public NotesResponse(NotesAction action) => Action = action;
}
