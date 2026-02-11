using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using DevTools.Snapshot.Models;

namespace DevTools.Snapshot.Engine;

internal sealed class SnapshotRecursiveJsonWriter
{
    public async Task WriteAsync(string rootPath, string outFile, IReadOnlySet<string> ignoreDirs)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
            throw new ArgumentException("rootPath is required.", nameof(rootPath));

        if (!Directory.Exists(rootPath))
            throw new DirectoryNotFoundException($"RootPath not found: {rootPath}");

        var root = new DirectoryInfo(rootPath);
        var node = BuildNode(root, ignoreDirs);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var json = JsonSerializer.Serialize(node, options);
        Directory.CreateDirectory(Path.GetDirectoryName(outFile)!);
        await File.WriteAllTextAsync(outFile, json, new UTF8Encoding(false));
    }

    private static SnapshotRecursiveNode BuildNode(DirectoryInfo dir, IReadOnlySet<string> ignoreDirs)
    {
        var entries = new List<SnapshotRecursiveNode>();

        foreach (var item in dir.GetFileSystemInfos()
                     .Where(e => !ignoreDirs.Contains(e.Name))
                     .OrderBy(e => e is FileInfo ? 1 : 0)
                     .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase))
        {
            if (item is DirectoryInfo subDir)
            {
                entries.Add(BuildNode(subDir, ignoreDirs));
            }
            else if (item is FileInfo file)
            {
                entries.Add(new SnapshotRecursiveNode(file.Name, GetFileKind(file), null));
            }
        }

        return new SnapshotRecursiveNode(dir.Name, SnapshotFileKind.Directory, entries.Count == 0 ? null : entries);
    }

    private static SnapshotFileKind GetFileKind(FileInfo file)
    {
        if (string.IsNullOrEmpty(file.Extension))
            return SnapshotDefaults.TextFilesWithoutExtension.Contains(file.Name)
                ? SnapshotFileKind.File
                : SnapshotFileKind.Binary;

        if (SnapshotDefaults.ImageExtensions.Contains(file.Extension))
            return SnapshotFileKind.Image;

        if (SnapshotDefaults.BinaryExtensions.Contains(file.Extension))
            return SnapshotFileKind.Binary;

        return SnapshotFileKind.File;
    }
}
