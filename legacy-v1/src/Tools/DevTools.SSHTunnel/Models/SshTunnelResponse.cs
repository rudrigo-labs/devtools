namespace DevTools.SSHTunnel.Models;

public sealed record SshTunnelResponse(
    SshTunnelAction Action,
    TunnelState State,
    bool IsRunning,
    TunnelConfiguration? Configuration,
    int? ProcessId,
    string? LastError);


