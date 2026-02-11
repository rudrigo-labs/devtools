namespace DevTools.Utf8Convert.Abstractions;

public interface IUtf8FileSystem
{
    bool FileExists(string path);
    IEnumerable<string> EnumerateFiles(string rootPath, bool recursive);
    Task<byte[]> ReadAllBytesAsync(string path, CancellationToken ct = default);
    Task WriteAllBytesAsync(string path, byte[] content, CancellationToken ct = default);
    void CopyFile(string sourcePath, string destinationPath, bool overwrite);
}
