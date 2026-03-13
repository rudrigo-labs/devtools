using DevTools.Core.Models;

namespace DevTools.Ngrok.Models;

/// <summary>
/// Configuração nomeada do Ngrok.
/// Herda NamedConfiguration — persistida via ToolConfigurationEntity.
/// Substitui NgrokSettings + NgrokSqliteSettingsStore + NgrokJsonSettingsStore.
/// </summary>
public sealed class NgrokEntity : NamedConfiguration
{
    public string AuthToken { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
    public string AdditionalArgs { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "http://127.0.0.1:4040/";
}

public enum NgrokAction
{
    ListTunnels = 0,
    CloseTunnel = 1,
    StartHttp   = 2,
    KillAll     = 3,
    Status      = 4
}

public sealed record TunnelInfo(string Name, string Proto, string PublicUrl, string? Addr);
public sealed record TunnelGroup(string BaseName, TunnelInfo? Http, TunnelInfo? Https);
public sealed record NgrokStartOptions(string Protocol, int Port, string? ExecutablePath = null, IReadOnlyList<string>? ExtraArgs = null);

public sealed class NgrokRequest
{
    public NgrokAction Action { get; set; }
    public string? BaseUrl { get; set; }
    public int TimeoutSeconds { get; set; } = 5;
    public int RetryCount { get; set; } = 1;
    public string? TunnelName { get; set; }
    public NgrokStartOptions? StartOptions { get; set; }
}

public sealed record NgrokResult(
    NgrokAction Action,
    string BaseUrl,
    IReadOnlyList<TunnelInfo>? Tunnels = null,
    IReadOnlyList<TunnelGroup>? Groups = null,
    bool? Closed = null,
    int? ProcessId = null,
    int? Killed = null,
    bool? HasAny = null);

public sealed record NgrokSelectionOption(string Label, NgrokEntity? Entity);
