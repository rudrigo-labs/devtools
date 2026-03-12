using DevTools.Rename.Abstractions;
using DevTools.Rename.Models;

namespace DevTools.Rename.Providers;

public sealed class DirectoryScanner
{
    private readonly IRenameFileSystem _fs;

    public DirectoryScanner(IRenameFileSystem fs)
    {
        _fs = fs;
    }

    public ScanResult Scan(string rootPath, RenameRequest request)
    {
        var ignoredDirs = new HashSet<string>(
            request.IgnoredDirectories.Select(d => d.Trim()),
            StringComparer.OrdinalIgnoreCase);

        var ignoredExts = new HashSet<string>(
            request.IgnoredExtensions.Select(e => e.Trim()),
            StringComparer.OrdinalIgnoreCase);

        var includedExts = request.IncludedExtensions.Count > 0
            ? new HashSet<string>(request.IncludedExtensions.Select(e => e.Trim()), StringComparer.OrdinalIgnoreCase)
            : null;

        var files = new List<string>();
        var directories = new List<string>();

        Recurse(rootPath, ignoredDirs, ignoredExts, includedExts, files, directories);

        return new ScanResult(files, directories);
    }

    private void Recurse(
        string current,
        HashSet<string> ignoredDirs,
        HashSet<string> ignoredExts,
        HashSet<string>? includedExts,
        List<string> files,
        List<string> directories)
    {
        foreach (var dir in _fs.EnumerateDirectories(current, "*", SearchOption.TopDirectoryOnly))
        {
            var name = Path.GetFileName(dir);
            if (ignoredDirs.Contains(name))
                continue;

            directories.Add(dir);
            Recurse(dir, ignoredDirs, ignoredExts, includedExts, files, directories);
        }

        foreach (var file in _fs.EnumerateFiles(current, "*", SearchOption.TopDirectoryOnly))
        {
            var ext = Path.GetExtension(file);
            if (ignoredExts.Contains(ext))
                continue;
            if (includedExts is not null && !includedExts.Contains(ext))
                continue;

            files.Add(file);
        }
    }
}

public sealed record ScanResult(
    IReadOnlyList<string> Files,
    IReadOnlyList<string> Directories);
