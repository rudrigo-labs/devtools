using System;
using System.IO;

namespace DevTools.Presentation.Wpf.Models;

public static class NotesStorageDefaults
{
    public static string GetDefaultPath()
    {
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        return Path.Combine(documents, "DevTools", "Notes");
    }
}

public class NotesSettings
{
    public string StoragePath { get; set; } = NotesStorageDefaults.GetDefaultPath();
    public string DefaultFormat { get; set; } = ".txt"; // .txt ou .md
    public bool AutoCloudSync { get; set; } = false;
    public string InitialListDisplay { get; set; } = "Auto"; // Auto | 8 | 15 | 20
}

public class GoogleDriveSettings
{
    public bool IsEnabled { get; set; } = false;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public string FolderName { get; set; } = "DevToolsNotes";
}
