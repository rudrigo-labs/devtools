namespace DevTools.Ngrok.Models;

public sealed record TunnelGroup(
    string BaseName,
    TunnelInfo? Http,
    TunnelInfo? Https
);
