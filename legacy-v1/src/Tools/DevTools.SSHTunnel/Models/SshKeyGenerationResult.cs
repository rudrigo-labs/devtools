namespace DevTools.SSHTunnel.Models;

public sealed record SshKeyGenerationResult(
    string PrivateKeyPath,
    string PublicKeyPath);
