using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace DevTools.Notes.Providers;

public static class NotesSlug
{
    public static string ToSlug(string? title)
    {
        var s = (title ?? string.Empty).Trim();
        if (s.Length == 0)
            return "nota";

        s = s.ToLowerInvariant();
        s = RemoveDiacritics(s);
        s = Regex.Replace(s, "[^a-z0-9]+", "-");
        s = s.Trim('-');
        return s.Length == 0 ? "nota" : s;
    }

    private static string RemoveDiacritics(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);

        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
