using DevTools.Core.Abstractions;

namespace DevTools.Core.Providers;

public sealed class SystemFileSystem : IFileSystem
{
    public bool FileExists(string path) => File.Exists(path);
    public bool DirectoryExists(string path) => Directory.Exists(path);

    public string ReadAllText(string path) => File.ReadAllText(path);

    public Task<string> ReadAllTextAsync(string path, CancellationToken ct = default)
        => File.ReadAllTextAsync(path, ct);

    public byte[] ReadAllBytes(string path) => File.ReadAllBytes(path);

    public Task<byte[]> ReadAllBytesAsync(string path, CancellationToken ct = default)
        => File.ReadAllBytesAsync(path, ct);

    public void WriteAllText(string path, string content) => File.WriteAllText(path, content);

    public Task WriteAllTextAsync(string path, string content, CancellationToken ct = default)
        => File.WriteAllTextAsync(path, content, ct);

    public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption option)
        => Directory.EnumerateFiles(path, searchPattern, option);

    public IEnumerable<string> EnumerateDirectories(string path, string searchPattern, SearchOption option)
        => Directory.EnumerateDirectories(path, searchPattern, option);

    public void CreateDirectory(string path) => Directory.CreateDirectory(path);

    public void CopyFile(string sourcePath, string destinationPath, bool overwrite)
        => File.Copy(sourcePath, destinationPath, overwrite);
}
