namespace DevTools.Presentation.Wpf.Models;

public class AppSettings
{
    public string? LastHarvestSourcePath { get; set; }
    public string? LastHarvestOutputPath { get; set; }
    public string? LastOrganizerInputPath { get; set; }
    
    // Notes Window Persistence
    public double? NotesWindowTop { get; set; }
    public double? NotesWindowLeft { get; set; }
    public double? NotesWindowWidth { get; set; }
    public double? NotesWindowHeight { get; set; }
    public string? NotesStoragePath { get; set; }

    // Cloud Keys (User provided)
    public string? GoogleClientId { get; set; }
    public string? GoogleClientSecret { get; set; }
    public string? OneDriveClientId { get; set; }
    public string? LastCloudProvider { get; set; }
    public DateTime? LastSyncTime { get; set; }

    // SshTunnel Persistence
    public string? LastImageSplitInputPath { get; set; }
    public string? LastImageSplitOutputDir { get; set; }
    public double? ImageSplitWindowTop { get; set; }
    public double? ImageSplitWindowLeft { get; set; }

    // Rename Persistence
    public string? LastRenameRootPath { get; set; }
    public double? RenameWindowTop { get; set; }
    public double? RenameWindowLeft { get; set; }

    // Snapshot Persistence
    public string? LastSnapshotRootPath { get; set; }
    public double? SnapshotWindowTop { get; set; }
    public double? SnapshotWindowLeft { get; set; }

    // Utf8Convert Persistence
    public string? LastUtf8RootPath { get; set; }
    public double? Utf8WindowTop { get; set; }
    public double? Utf8WindowLeft { get; set; }

    // SshTunnel Persistence
    public double? SshWindowTop { get; set; }
    public double? SshWindowLeft { get; set; }

    // Ngrok Persistence
    public double? NgrokWindowTop { get; set; }
    public double? NgrokWindowLeft { get; set; }

    // SearchText Persistence
    public string? LastSearchTextRootPath { get; set; }
    public string? LastSearchTextInclude { get; set; }
    public string? LastSearchTextExclude { get; set; }
    public double? SearchTextWindowTop { get; set; }
    public double? SearchTextWindowLeft { get; set; }

    // Migrations Persistence
    public string? LastMigrationsRootPath { get; set; }
    public string? LastMigrationsStartupPath { get; set; }
    public string? LastMigrationsDbContext { get; set; }
    public double? MigrationsWindowTop { get; set; }
    public double? MigrationsWindowLeft { get; set; }
}
