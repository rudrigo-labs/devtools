namespace DevTools.Rename.Abstractions;

public interface IRenameFileSystem
{
    bool FileExists(string path);
    bool DirectoryExists(string path);
    Task<byte[]> ReadAllBytesAsync(string path, CancellationToken ct = default);
    Task WriteAllBytesAsync(string path, byte[] content, CancellationToken ct = default);
    void CreateDirectory(string path);
    IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption option);
    IEnumerable<string> EnumerateDirectories(string path, string searchPattern, SearchOption option);
    void MoveFile(string sourcePath, string destinationPath);
    void MoveDirectory(string sourcePath, string destinationPath);
    void CopyFile(string sourcePath, string destinationPath, bool overwrite);
}
