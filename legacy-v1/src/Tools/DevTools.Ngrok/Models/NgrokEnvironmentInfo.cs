namespace DevTools.Ngrok.Models;

public sealed class NgrokEnvironmentInfo
{
    public bool NgrokInstalled { get; set; }
    public bool IsConfigured { get; set; }
    public string ExecutablePath { get; set; } = string.Empty;
    public string ConfigPath { get; set; } = string.Empty;
    public string Authtoken { get; set; } = string.Empty;
}
