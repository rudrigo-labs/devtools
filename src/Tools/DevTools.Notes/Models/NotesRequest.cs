namespace DevTools.Notes.Models;

public sealed record NotesRequest(
    NotesAction Action,
    string? NoteKey = null,
    string? Content = null,
    string? NotesRootPath = null,
    string? ConfigPath = null,
    bool Overwrite = true,

    // Modo Simples
    string? Title = null,
    DateTime? LocalDate = null,
    string? OutputPath = null,
    string? ZipPath = null,
    bool UseMarkdown = true,
    bool CreateDateFolder = true,

    // Cloud
    DevTools.Notes.Cloud.CloudProviderType CloudProvider = DevTools.Notes.Cloud.CloudProviderType.None,
    DevTools.Notes.Cloud.CloudConfiguration? CloudConfig = null);
