namespace DevTools.SSHTunnel.Models;

public sealed record SshTunnelRequest(
    SshTunnelAction Action,
    TunnelConfiguration? Configuration = null);

