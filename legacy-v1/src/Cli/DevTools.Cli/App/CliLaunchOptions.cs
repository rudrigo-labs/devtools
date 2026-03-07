namespace DevTools.Cli.App;

public sealed class CliLaunchOptions
{
    public string? CommandName { get; set; }
    public Dictionary<string, string> Options { get; } = new(StringComparer.OrdinalIgnoreCase);
    public bool IsNonInteractive { get; set; }

    public string? GetOption(string key)
    {
        if (Options.TryGetValue(key, out var value))
            return value;
        return null;
    }

    public bool HasFlag(string key)
    {
        return Options.ContainsKey(key);
    }
}
