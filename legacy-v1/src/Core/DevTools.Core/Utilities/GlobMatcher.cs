using System.Text.RegularExpressions;

namespace DevTools.Core.Utilities;

public sealed class GlobMatcher
{
    private readonly Regex _regex;

    public GlobMatcher(string pattern)
    {
        Pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
        _regex = new Regex("^" + GlobToRegex(Normalize(pattern)) + "$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    public string Pattern { get; }

    public bool IsMatch(string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        return _regex.IsMatch(Normalize(path));
    }

    public static bool IsMatch(string path, string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return false;

        var regex = "^" + GlobToRegex(Normalize(pattern)) + "$";
        return Regex.IsMatch(Normalize(path), regex, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static string Normalize(string value)
        => value.Replace(Path.DirectorySeparatorChar, '/')
                .Replace(Path.AltDirectorySeparatorChar, '/');

    private static string GlobToRegex(string glob)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < glob.Length; i++)
        {
            var c = glob[i];
            if (c == '*')
            {
                var isDoubleStar = i + 1 < glob.Length && glob[i + 1] == '*';
                if (isDoubleStar)
                {
                    sb.Append(".*");
                    i++;
                }
                else
                {
                    sb.Append("[^/]*");
                }
            }
            else if (c == '?')
            {
                sb.Append("[^/]");
            }
            else
            {
                sb.Append(Regex.Escape(c.ToString()));
            }
        }
        return sb.ToString();
    }
}
