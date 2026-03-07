namespace DevTools.Ngrok.Models;

public sealed record TunnelInfo(
    string Name,
    string Proto,
    string PublicUrl,
    string? Addr
);
