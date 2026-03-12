using DevTools.Core.Models;

namespace DevTools.Rename.Models;

public sealed class RenameRequest : FileToolOptions
{
    public string OldText { get; set; } = string.Empty;
    public string NewText { get; set; } = string.Empty;
    public RenameMode Mode { get; set; } = RenameMode.General;
    public bool DryRun { get; set; }
    public bool BackupEnabled { get; set; } = true;
    public bool WriteUndoLog { get; set; } = true;
    public string? UndoLogPath { get; set; }
    public string? ReportPath { get; set; }
    public int MaxDiffLinesPerFile { get; set; } = 200;

    public RenameRequest()
    {
        IgnoredDirectories = RenameDefaults.DefaultIgnoredDirectories;
        IgnoredExtensions = RenameDefaults.DefaultIgnoredExtensions;
        IncludedExtensions = RenameDefaults.DefaultIncludedExtensions;
    }
}
