using System.IO.Compression;
using DevTools.Core.Abstractions;
using DevTools.Core.Providers;
using DevTools.Core.Results;

namespace DevTools.Notes.Providers;

public sealed class NotesBackupStore
{
    private readonly IFileSystem _fs;

    public NotesBackupStore(IFileSystem? fileSystem = null)
    {
        _fs = fileSystem ?? new SystemFileSystem();
    }

    public RunResult<string> ExportZip(string rootPath, string? outputPath)
    {
        try
        {
            var root = NotesPaths.ResolveRoot(rootPath);
            var itemsDir = NotesPaths.ItemsDir(root);
            var indexPath = NotesPaths.IndexPath(root);

            _fs.CreateDirectory(itemsDir);

            var targetDir = string.IsNullOrWhiteSpace(outputPath)
                ? NotesPaths.ExportsDir(root)
                : Path.GetFullPath(outputPath);

            _fs.CreateDirectory(targetDir);
            var zipName = $"DevToolsNotes_{DateTime.Now:yyyyMMdd-HHmmss}.zip";
            var zipPath = Path.Combine(targetDir, zipName);

            if (File.Exists(zipPath))
                File.Delete(zipPath);

            using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);

            if (Directory.Exists(itemsDir))
            {
                foreach (var file in Directory.EnumerateFiles(itemsDir, "*", SearchOption.AllDirectories))
                {
                    var rel = Path.GetRelativePath(root, file).Replace('\\', '/');
                    zip.CreateEntryFromFile(file, rel, CompressionLevel.Optimal);
                }
            }

            if (_fs.FileExists(indexPath))
                zip.CreateEntryFromFile(indexPath, "index.json", CompressionLevel.Optimal);

            return RunResult<string>.Success(zipPath);
        }
        catch (Exception ex)
        {
            return RunResult<string>.Fail(new ErrorDetail(
                "notes.backup.export.failed",
                "Failed to export notes zip.",
                ex.Message,
                ex));
        }
    }

    public RunResult<(string TempDir, List<string> ItemFiles, bool HasIndex)> ExtractZip(string zipPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(zipPath) || !File.Exists(zipPath))
                return RunResult<(string, List<string>, bool)>.Fail(new ErrorDetail("notes.backup.zip.missing", "ZIP not found.", zipPath));

            var tempDir = Path.Combine(Path.GetTempPath(), "DevToolsNotes", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            ZipFile.ExtractToDirectory(zipPath, tempDir, true);

            var itemsDir = Path.Combine(tempDir, "items");
            var itemFiles = Directory.Exists(itemsDir)
                ? Directory.EnumerateFiles(itemsDir, "*", SearchOption.AllDirectories).ToList()
                : new List<string>();

            var hasIndex = File.Exists(Path.Combine(tempDir, "index.json"));
            return RunResult<(string, List<string>, bool)>.Success((tempDir, itemFiles, hasIndex));
        }
        catch (Exception ex)
        {
            return RunResult<(string, List<string>, bool)>.Fail(new ErrorDetail(
                "notes.backup.import.extract.failed",
                "Failed to extract zip.",
                ex.Message,
                ex));
        }
    }
}
