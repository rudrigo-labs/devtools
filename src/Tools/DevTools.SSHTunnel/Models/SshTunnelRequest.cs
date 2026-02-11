namespace DevTools.SSHTunnel.Models;

public sealed record SshTunnelRequest(
    SshTunnelAction Action,
    TunnelProfile? Profile = null);
