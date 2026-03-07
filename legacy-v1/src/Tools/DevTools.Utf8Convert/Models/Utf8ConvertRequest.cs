namespace DevTools.Utf8Convert.Models;

public sealed record Utf8ConvertRequest(
    string RootPath,
    bool Recursive = true,
    bool DryRun = false,
    bool CreateBackup = true,
    bool OutputBom = true,
    IReadOnlyList<string>? IncludeGlobs = null,
    IReadOnlyList<string>? ExcludeGlobs = null);
