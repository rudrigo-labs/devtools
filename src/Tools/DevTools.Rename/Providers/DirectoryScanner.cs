using DevTools.Rename.Abstractions;
using DevTools.Core.Utilities;

namespace DevTools.Rename.Providers;

public sealed class DirectoryScanner
{
    private readonly IRenameFileSystem _fs;

    public DirectoryScanner(IRenameFileSystem fs)
    {
        _fs = fs;
    }

    public ScanResult Scan(string rootPath, PathFilter filter)
    {
        var files = new List<string>();
        var directories = new List<string>();

        Recurse(rootPath, rootPath, filter, files, directories);

        return new ScanResult(files, directories);
    }

    private void Recurse(
        string rootPath,
        string current,
        PathFilter filter,
        List<string> files,
        List<string> directories)
    {
        foreach (var dir in _fs.EnumerateDirectories(current, "*", SearchOption.TopDirectoryOnly))
        {
            var relative = Path.GetRelativePath(rootPath, dir);
            var normalized = Normalize(relative);

            if (filter.IsExcluded(normalized))
                continue;

            directories.Add(dir);
            Recurse(rootPath, dir, filter, files, directories);
        }

        foreach (var file in _fs.EnumerateFiles(current, "*", SearchOption.TopDirectoryOnly))
        {
            var relative = Path.GetRelativePath(rootPath, file);
            var normalized = Normalize(relative);

            if (filter.IsExcluded(normalized))
                continue;

            files.Add(file);
        }
    }

    private static string Normalize(string path)
        => path.Replace('\\', '/');
}

public sealed record ScanResult(
    IReadOnlyList<string> Files,
    IReadOnlyList<string> Directories);
