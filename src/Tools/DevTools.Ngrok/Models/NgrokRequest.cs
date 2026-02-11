namespace DevTools.Ngrok.Models;

public sealed record NgrokRequest(
    NgrokAction Action,
    string? BaseUrl = null,
    int TimeoutSeconds = 5,
    int RetryCount = 1,
    string? TunnelName = null,
    NgrokStartOptions? StartOptions = null
);
