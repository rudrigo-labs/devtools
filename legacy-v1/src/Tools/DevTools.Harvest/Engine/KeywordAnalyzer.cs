using System.Text.RegularExpressions;
using DevTools.Harvest.Configuration;
using DevTools.Harvest.Models;

namespace DevTools.Harvest.Engine;

internal static class KeywordAnalyzer
{
    public static IReadOnlyList<KeywordDensity> Analyze(
        string content,
        int lineCount,
        IEnumerable<HarvestKeywordCategory> categories,
        int densityScale)
    {
        var results = new List<KeywordDensity>();
        var safeLines = Math.Max(1, lineCount);

        foreach (var category in categories)
        {
            var hits = 0;
            foreach (var keyword in category.Keywords)
                hits += CountKeyword(content, keyword);

            var density = hits == 0
                ? 0
                : (hits / (double)safeLines) * densityScale;

            results.Add(new KeywordDensity(category.Name, hits, density));
        }

        return results;
    }

    private static int CountKeyword(string content, string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return 0;

        var pattern = Regex.Escape(keyword);
        if (IsWord(keyword))
            pattern = $@"\b{pattern}\b";

        return Regex.Matches(content, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).Count;
    }

    private static bool IsWord(string value)
    {
        foreach (var c in value)
        {
            if (!(char.IsLetterOrDigit(c) || c == '_'))
                return false;
        }

        return true;
    }
}
