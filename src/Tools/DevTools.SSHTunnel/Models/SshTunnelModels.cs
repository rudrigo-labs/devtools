namespace DevTools.SSHTunnel.Models;

public sealed class TunnelConfiguration
{
    public string Name { get; set; } = "default";
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

    public static TunnelConfiguration FromEntity(SshTunnelEntity entity) => new()
    {
        Name                  = entity.Name,
        SshHost               = entity.SshHost,
        SshPort               = entity.SshPort,
        SshUser               = entity.SshUser,
        LocalBindHost         = entity.LocalBindHost,
        LocalPort             = entity.LocalPort,
        RemoteHost            = entity.RemoteHost,
        RemotePort            = entity.RemotePort,
        IdentityFile          = entity.IdentityFile,
        StrictHostKeyChecking = entity.StrictHostKeyChecking,
        ConnectTimeoutSeconds = entity.ConnectTimeoutSeconds
    };
}

public sealed record SshKeyGenerationResult(string PrivateKeyPath, string PublicKeyPath);

public sealed class SshTunnelRequest
{
    public SshTunnelAction Action { get; set; }
    public TunnelConfiguration? Configuration { get; set; }
}

public sealed record SshTunnelResult(
    SshTunnelAction Action,
    TunnelState State,
    bool IsRunning,
    TunnelConfiguration? Configuration,
    int? ProcessId,
    string? LastError);

public sealed record SshTunnelSelectionOption(string Label, SshTunnelEntity? Entity);
