namespace DevTools.Notes.Models;

public sealed record NotesResponse(
    NotesAction Action,
    NoteReadResult? ReadResult = null,
    NoteWriteResult? WriteResult = null,
    string? ConfigPath = null,

    // Modo Simples
    NoteCreateResult? CreateResult = null,
    NoteListResult? ListResult = null,
    NoteDeleteResult? DeleteResult = null,
    NotesBackupReport? BackupReport = null,
    string? ExportedZipPath = null);
