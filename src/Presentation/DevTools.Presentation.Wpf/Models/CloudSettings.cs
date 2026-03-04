namespace DevTools.Presentation.Wpf.Models;

public class NotesSettings
{
    public string StoragePath { get; set; } = string.Empty;
    public string DefaultFormat { get; set; } = ".txt"; // .txt ou .md
    public bool AutoCloudSync { get; set; } = false;
}

public class GoogleDriveSettings
{
    public bool IsEnabled { get; set; } = false;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public string FolderName { get; set; } = "DevToolsNotes";
}
