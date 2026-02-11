using System.Text.RegularExpressions;

namespace DevTools.Notes.Providers;

public static class EnvironmentVariableExpander
{
    private static readonly Regex Pattern = new(@"\$\{(\w+)\}", RegexOptions.Compiled);

    public static string Expand(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return Pattern.Replace(input, match =>
        {
            var key = match.Groups[1].Value;
            return Environment.GetEnvironmentVariable(key) ?? match.Value;
        });
    }
}
