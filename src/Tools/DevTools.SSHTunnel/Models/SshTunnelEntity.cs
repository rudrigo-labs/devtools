using DevTools.Core.Models;

namespace DevTools.SSHTunnel.Models;

/// <summary>
/// Configuração nomeada do SSHTunnel.
/// Herda NamedConfiguration — persistida via ToolConfigurationEntity.
/// </summary>
public sealed class SshTunnelEntity : NamedConfiguration
{
    public string SshHost { get; set; } = string.Empty;
    public int SshPort { get; set; } = 22;
    public string SshUser { get; set; } = string.Empty;
    public string LocalBindHost { get; set; } = "127.0.0.1";
    public int LocalPort { get; set; } = 14331;
    public string RemoteHost { get; set; } = "127.0.0.1";
    public int RemotePort { get; set; } = 1433;
    public string? IdentityFile { get; set; }
    public SshStrictHostKeyChecking StrictHostKeyChecking { get; set; } = SshStrictHostKeyChecking.Default;
    public int? ConnectTimeoutSeconds { get; set; }
}
