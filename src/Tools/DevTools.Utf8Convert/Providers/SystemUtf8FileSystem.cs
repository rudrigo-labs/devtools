using DevTools.Utf8Convert.Abstractions;

namespace DevTools.Utf8Convert.Providers;

public sealed class SystemUtf8FileSystem : IUtf8FileSystem
{
    public bool FileExists(string path) => File.Exists(path);

    public IEnumerable<string> EnumerateFiles(string rootPath, bool recursive)
        => Directory.EnumerateFiles(rootPath, "*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

    public Task<byte[]> ReadAllBytesAsync(string path, CancellationToken ct = default)
        => File.ReadAllBytesAsync(path, ct);

    public Task WriteAllBytesAsync(string path, byte[] content, CancellationToken ct = default)
        => File.WriteAllBytesAsync(path, content, ct);

    public void CopyFile(string sourcePath, string destinationPath, bool overwrite)
        => File.Copy(sourcePath, destinationPath, overwrite);
}
