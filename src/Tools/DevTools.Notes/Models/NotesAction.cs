namespace DevTools.Notes.Models;

public enum NotesAction
{
    LoadNote = 0,
    SaveNote = 1,

    // Modo Simples (filesystem + export/import ZIP)
    CreateItem = 10,
    ListItems = 11,
    ExportZip = 12,
    ImportZip = 13
}
