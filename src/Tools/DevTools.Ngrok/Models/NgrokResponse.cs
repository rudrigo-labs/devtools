namespace DevTools.Ngrok.Models;

public sealed record NgrokResponse(
    NgrokAction Action,
    string BaseUrl,
    IReadOnlyList<TunnelInfo>? Tunnels = null,
    IReadOnlyList<TunnelGroup>? Groups = null,
    bool? Closed = null,
    int? ProcessId = null,
    int? Killed = null,
    bool? HasAny = null
);
