using System.Text.RegularExpressions;

namespace DevTools.SearchText.Engine;

internal sealed class TextMatcher
{
    private readonly Regex _regex;

    public TextMatcher(string pattern, bool useRegex, bool caseSensitive, bool wholeWord)
    {
        if (!useRegex)
        {
            pattern = Regex.Escape(pattern);
            if (wholeWord)
                pattern = $@"\b{pattern}\b";
        }
        else if (wholeWord)
        {
            pattern = $@"\b(?:{pattern})\b";
        }

        var options = RegexOptions.Compiled | RegexOptions.CultureInvariant;
        if (!caseSensitive)
            options |= RegexOptions.IgnoreCase;

        _regex = new Regex(pattern, options);
    }

    public IEnumerable<(int Index, int Length)> FindMatches(string line)
    {
        foreach (Match match in _regex.Matches(line))
        {
            if (!match.Success) continue;
            yield return (match.Index, match.Length);
        }
    }
}
