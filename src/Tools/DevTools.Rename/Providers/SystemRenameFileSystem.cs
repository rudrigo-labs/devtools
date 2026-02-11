using DevTools.Rename.Abstractions;

namespace DevTools.Rename.Providers;

public sealed class SystemRenameFileSystem : IRenameFileSystem
{
    public bool FileExists(string path) => File.Exists(path);
    public bool DirectoryExists(string path) => Directory.Exists(path);

    public Task<byte[]> ReadAllBytesAsync(string path, CancellationToken ct = default)
        => File.ReadAllBytesAsync(path, ct);

    public Task WriteAllBytesAsync(string path, byte[] content, CancellationToken ct = default)
        => File.WriteAllBytesAsync(path, content, ct);

    public void CreateDirectory(string path) => Directory.CreateDirectory(path);

    public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption option)
        => Directory.EnumerateFiles(path, searchPattern, option);

    public IEnumerable<string> EnumerateDirectories(string path, string searchPattern, SearchOption option)
        => Directory.EnumerateDirectories(path, searchPattern, option);

    public void MoveFile(string sourcePath, string destinationPath)
        => File.Move(sourcePath, destinationPath);

    public void MoveDirectory(string sourcePath, string destinationPath)
        => Directory.Move(sourcePath, destinationPath);

    public void CopyFile(string sourcePath, string destinationPath, bool overwrite)
        => File.Copy(sourcePath, destinationPath, overwrite);
}
