using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using DevTools.Harvest.Models;

namespace DevTools.Harvest.Engine;

internal static class KeywordAnalyzer
{
    private static readonly ConcurrentDictionary<string, Regex> RegexCache = new(StringComparer.OrdinalIgnoreCase);

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
                ? 0.0
                : (hits / (double)safeLines) * densityScale;

            results.Add(new KeywordDensity(category.Name, hits, density));
        }

        return results;
    }

    private static int CountKeyword(string content, string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return 0;

        var regex = RegexCache.GetOrAdd(keyword, k =>
        {
            var pattern = Regex.Escape(k);
            if (IsWord(k))
                pattern = $@"\b{pattern}\b";

            return new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        });

        return regex.Matches(content).Count;
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
