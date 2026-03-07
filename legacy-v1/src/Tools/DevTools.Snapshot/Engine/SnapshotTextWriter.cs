using System.Text;

namespace DevTools.Snapshot.Engine;

internal sealed class SnapshotTextWriter
{
    public async Task WriteAsync(string rootPath, string outFile, IReadOnlySet<string> ignoreDirs)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
            throw new ArgumentException("rootPath is required.", nameof(rootPath));

        if (!Directory.Exists(rootPath))
            throw new DirectoryNotFoundException($"RootPath not found: {rootPath}");

        var sb = new StringBuilder();
        var root = new DirectoryInfo(rootPath);

        sb.AppendLine($"Snapshot: {DateTimeOffset.UtcNow:O}");
        sb.AppendLine($"Project: {root.Name}");
        sb.AppendLine(new string('-', 44));
        sb.AppendLine(root.Name);

        void Walk(string path, string prefix)
        {
            var entries = new DirectoryInfo(path).GetFileSystemInfos()
                .Where(e => !ignoreDirs.Contains(e.Name))
                .OrderBy(e => e is FileInfo ? 1 : 0)
                .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            for (int i = 0; i < entries.Count; i++)
            {
                var isLast = i == entries.Count - 1;
                sb.AppendLine($"{prefix}{(isLast ? "└── " : "├── ")}{entries[i].Name}");

                if (entries[i] is DirectoryInfo dir)
                    Walk(dir.FullName, prefix + (isLast ? "    " : "│   "));
            }
        }

        Walk(rootPath, "");

        Directory.CreateDirectory(Path.GetDirectoryName(outFile)!);
        await File.WriteAllTextAsync(outFile, sb.ToString(), new UTF8Encoding(false));
    }
}
