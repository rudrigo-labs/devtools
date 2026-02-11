using System.Globalization;
using System.Text;

namespace DevTools.Organizer.Engine;

internal static class TextNormalizer
{
    public static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = value.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(c);
            if (uc == UnicodeCategory.NonSpacingMark)
                continue;

            sb.Append(char.ToLowerInvariant(c));
        }

        return CompressWhitespace(sb.ToString());
    }

    private static string CompressWhitespace(string value)
    {
        var sb = new StringBuilder(value.Length);
        var wasSpace = false;

        foreach (var c in value)
        {
            if (char.IsWhiteSpace(c))
            {
                if (wasSpace) continue;
                sb.Append(' ');
                wasSpace = true;
                continue;
            }

            wasSpace = false;
            sb.Append(c);
        }

        return sb.ToString().Trim();
    }
}
