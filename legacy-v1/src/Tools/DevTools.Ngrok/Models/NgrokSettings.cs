namespace DevTools.Ngrok.Models;

public class NgrokSettings
{
    public string ExecutablePath { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;
    public string AdditionalArgs { get; set; } = string.Empty;

    public void Normalize()
    {
        ExecutablePath ??= string.Empty;
        AuthToken ??= string.Empty;
        AdditionalArgs ??= string.Empty;
    }
}
