using DevTools.Rename.Models;

namespace DevTools.Rename.Providers;

public static class DiffGenerator
{
    public static FileDiffSummary Generate(string path, string oldContent, string newContent, int maxLines)
    {
        var oldLines = SplitLines(oldContent);
        var newLines = SplitLines(newContent);
        var max = Math.Max(oldLines.Length, newLines.Length);
        var changes = new List<LineChange>();

        for (var i = 0; i < max; i++)
        {
            var oldLine = i < oldLines.Length ? oldLines[i] : null;
            var newLine = i < newLines.Length ? newLines[i] : null;

            if (string.Equals(oldLine, newLine, StringComparison.Ordinal))
                continue;

            changes.Add(new LineChange(i + 1, oldLine, newLine));
            if (changes.Count >= maxLines)
                break;
        }

        return new FileDiffSummary(path, changes);
    }

    private static string[] SplitLines(string content)
        => content.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
}
