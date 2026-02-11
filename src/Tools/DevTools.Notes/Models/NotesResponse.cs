namespace DevTools.Notes.Models;

public sealed record NotesResponse(
    NotesAction Action,
    NoteReadResult? ReadResult = null,
    NoteWriteResult? WriteResult = null,
    string? ConfigPath = null,

    // Modo Simples
    NoteCreateResult? CreateResult = null,
    NoteListResult? ListResult = null,
    NotesBackupReport? BackupReport = null,
    string? ExportedZipPath = null,

    // Cloud
    DevTools.Notes.Cloud.SyncResult? SyncResult = null,
    bool IsConnected = false,
    string? CloudUser = null);
