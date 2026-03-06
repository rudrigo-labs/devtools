using DevTools.Core.Abstractions;
using DevTools.SSHTunnel.Models;

namespace DevTools.SSHTunnel.Engine;

public sealed class SshKeyService
{
    private readonly IProcessRunner _runner;

    public SshKeyService(IProcessRunner runner)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
    }

    public async Task<SshKeyGenerationResult> GenerateAsync(
        string? directoryPath = null,
        string filePrefix = "devtools_ed25519",
        CancellationToken ct = default)
    {
        var targetDirectory = ResolveDirectory(directoryPath);
        Directory.CreateDirectory(targetDirectory);

        var privateKeyPath = GetUniquePrivateKeyPath(targetDirectory, filePrefix);
        var comment = $"devtools@{Environment.MachineName}".ToLowerInvariant();
        var arguments = $"-t ed25519 -f \"{privateKeyPath}\" -N \"\" -C \"{comment}\"";

        try
        {
            var result = await _runner.RunAsync("ssh-keygen", arguments, ct: ct).ConfigureAwait(false);
            if (result.ExitCode != 0)
            {
                throw new SshTunnelConfigException(
                    "sshtunnel.keygen.failed",
                    "Falha ao gerar chave SSH.",
                    string.IsNullOrWhiteSpace(result.StdErr) ? result.StdOut : result.StdErr);
            }
        }
        catch (SshTunnelException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new SshTunnelConfigException(
                "sshtunnel.keygen.not_available",
                "Nao foi possivel executar ssh-keygen. Verifique se o OpenSSH esta instalado.",
                ex.Message,
                ex);
        }

        return new SshKeyGenerationResult(privateKeyPath, $"{privateKeyPath}.pub");
    }

    private static string ResolveDirectory(string? directoryPath)
    {
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            return directoryPath;
        }

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(userProfile, ".ssh");
    }

    private static string GetUniquePrivateKeyPath(string directoryPath, string filePrefix)
    {
        var safePrefix = string.IsNullOrWhiteSpace(filePrefix) ? "devtools_ed25519" : filePrefix.Trim();
        var basePath = Path.Combine(directoryPath, safePrefix);

        if (!File.Exists(basePath) && !File.Exists($"{basePath}.pub"))
        {
            return basePath;
        }

        var index = 1;
        while (true)
        {
            var candidate = Path.Combine(directoryPath, $"{safePrefix}_{index}");
            if (!File.Exists(candidate) && !File.Exists($"{candidate}.pub"))
            {
                return candidate;
            }

            index++;
        }
    }
}

