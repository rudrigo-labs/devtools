namespace DevTools.SSHTunnel.Models;

public sealed class TunnelProfile
{
    public string Name { get; set; } = "default";

    // SSH target
    public string SshHost { get; set; } = string.Empty;
    public int SshPort { get; set; } = 22;
    public string SshUser { get; set; } = string.Empty;

    // Tunnel mapping
    public string LocalBindHost { get; set; } = "127.0.0.1";
    public int LocalPort { get; set; } = 14331;

    public string RemoteHost { get; set; } = "127.0.0.1";
    public int RemotePort { get; set; } = 1433;

    // Optional identity file (leave null/empty to use default ssh behavior)
    public string? IdentityFile { get; set; }

    // Optional SSH options
    public SshStrictHostKeyChecking StrictHostKeyChecking { get; set; } = SshStrictHostKeyChecking.Default;
    public int? ConnectTimeoutSeconds { get; set; }
}
