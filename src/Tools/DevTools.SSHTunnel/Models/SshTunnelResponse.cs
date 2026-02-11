namespace DevTools.SSHTunnel.Models;

public sealed record SshTunnelResponse(
    SshTunnelAction Action,
    TunnelState State,
    bool IsRunning,
    TunnelProfile? Profile,
    int? ProcessId,
    string? LastError);
