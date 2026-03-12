using System.Text;
using System.Text.RegularExpressions;

namespace DevTools.Core.Utilities;

public static class SlugNormalizer
{
    private static readonly Regex InvalidChars = new("[^a-z0-9-]", RegexOptions.Compiled);
    private static readonly Regex MultiDash = new("-{2,}", RegexOptions.Compiled);

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var ascii = RemoveDiacritics(value.Trim()).ToLowerInvariant();
        ascii = ascii.Replace(' ', '-').Replace('_', '-');
        ascii = InvalidChars.Replace(ascii, string.Empty);
        ascii = MultiDash.Replace(ascii, "-");
        return ascii.Trim('-');
    }

    public static bool IsValid(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return false;

        return Normalize(slug) == slug;
    }

    private static string RemoveDiacritics(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);

        foreach (var ch in normalized)
        {
            var category = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}

