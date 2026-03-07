using System.Text.Json;
using DevTools.Rename.Models;

namespace DevTools.Rename.Providers;

public static class RenameReportWriter
{
    public static void Write(string path, RenameReport report)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    public static void WriteUndoLog(string path, RenameUndoLog log)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(log, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }
}
