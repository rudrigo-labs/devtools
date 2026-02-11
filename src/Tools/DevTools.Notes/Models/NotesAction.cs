namespace DevTools.Notes.Models;

public enum NotesAction
{
    LoadNote = 0,
    SaveNote = 1,

    // Modo Simples (filesystem + export/import ZIP)
    CreateItem = 10,
    ListItems = 11,
    ExportZip = 12,
    ImportZip = 13,

    // Cloud
    ConnectGoogle = 20,
    ConnectOneDrive = 21,
    DisconnectCloud = 22,
    SyncCloud = 23,
    GetCloudStatus = 24
}
