namespace DevTools.Core.Abstractions;

public interface IFileSystem
{
    bool FileExists(string path);
    bool DirectoryExists(string path);

    string ReadAllText(string path);
    Task<string> ReadAllTextAsync(string path, CancellationToken ct = default);
    byte[] ReadAllBytes(string path);
    Task<byte[]> ReadAllBytesAsync(string path, CancellationToken ct = default);

    void WriteAllText(string path, string content);
    Task WriteAllTextAsync(string path, string content, CancellationToken ct = default);

    IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption option);
    IEnumerable<string> EnumerateDirectories(string path, string searchPattern, SearchOption option);

    void CreateDirectory(string path);
    void CopyFile(string sourcePath, string destinationPath, bool overwrite);
}
