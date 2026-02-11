namespace DevTools.Ngrok.Models;

public sealed record NgrokStartOptions(
    string Protocol,
    int Port,
    string? ExecutablePath = null,
    IReadOnlyList<string>? ExtraArgs = null
);
