namespace DevTools.Notes.Models;

public sealed class NotesRequest
{
    public NotesAction Action { get; set; }
    public string? NoteKey { get; set; }
    public string? Content { get; set; }
    public string? NotesRootPath { get; set; }
    public bool Overwrite { get; set; } = true;

    // Modo Simples
    public string? Title { get; set; }
    public string? Extension { get; set; }       // ".md" ou ".txt" — escolhido por nota
    public DateTime? LocalDate { get; set; }
    public string? OutputPath { get; set; }
    public string? ZipPath { get; set; }
    public bool CreateDateFolder { get; set; } = true;
}
