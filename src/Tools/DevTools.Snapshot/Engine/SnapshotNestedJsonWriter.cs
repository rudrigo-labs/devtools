using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using DevTools.Snapshot.Models;

namespace DevTools.Snapshot.Engine;

internal sealed class SnapshotNestedJsonWriter
{
    public async Task WriteAsync(string rootPath, string outFile, IReadOnlySet<string> ignoreDirs)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
            throw new ArgumentException("rootPath is required.", nameof(rootPath));

        if (!Directory.Exists(rootPath))
            throw new DirectoryNotFoundException($"RootPath not found: {rootPath}");

        var folders = new DirectoryInfo(rootPath)
            .EnumerateDirectories("*", SearchOption.AllDirectories)
            .Where(d => !IsIgnoredPath(d.FullName, ignoreDirs))
            .Select(d => new SnapshotFolder(
                Name: Path.GetRelativePath(rootPath, d.FullName).Replace('\\', '/'),
                Files: d.EnumerateFiles()
                        .Where(f => !ignoreDirs.Contains(f.Name))
                        .OrderBy(f => f.Name)
                        .Select(f => f.Name)
                        .ToList()
            ))
            .Where(f => f.Files.Count > 0)
            .OrderBy(f => f.Name)
            .ToList();

        var payload = new SnapshotNestedProject(Path.GetFileName(rootPath.TrimEnd(Path.DirectorySeparatorChar)), folders);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var json = JsonSerializer.Serialize(payload, options);
        Directory.CreateDirectory(Path.GetDirectoryName(outFile)!);
        await File.WriteAllTextAsync(outFile, json, new UTF8Encoding(false));
    }

    private static bool IsIgnoredPath(string fullPath, IReadOnlySet<string> ignoreDirs)
    {
        var parts = fullPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        foreach (var part in parts)
        {
            if (ignoreDirs.Contains(part))
                return true;
        }

        return false;
    }

    private sealed record SnapshotNestedProject(string Project, IReadOnlyList<SnapshotFolder> Folders);
}
