using DevTools.SSHTunnel.Models;

namespace DevTools.SSHTunnel.Engine;

public static class SshCommandBuilder
{
    public static string BuildArgs(TunnelProfile p)
    {
        var parts = new List<string>
        {
            "-N",
            "-L",
            $"{p.LocalBindHost}:{p.LocalPort}:{p.RemoteHost}:{p.RemotePort}"
        };

        if (p.SshPort != 22)
        {
            parts.Add("-p");
            parts.Add(p.SshPort.ToString());
        }

        if (!string.IsNullOrWhiteSpace(p.IdentityFile))
        {
            parts.Add("-i");
            parts.Add(ExpandHome(p.IdentityFile!));
        }

        if (p.StrictHostKeyChecking != SshStrictHostKeyChecking.Default)
        {
            parts.Add("-o");
            parts.Add($"StrictHostKeyChecking={ToStrictHostKeyChecking(p.StrictHostKeyChecking)}");

            if (p.StrictHostKeyChecking == SshStrictHostKeyChecking.No)
            {
                parts.Add("-o");
                parts.Add("UserKnownHostsFile=/dev/null");
            }
        }

        if (p.ConnectTimeoutSeconds.HasValue && p.ConnectTimeoutSeconds.Value > 0)
        {
            parts.Add("-o");
            parts.Add($"ConnectTimeout={p.ConnectTimeoutSeconds.Value}");
        }

        parts.Add("-o");
        parts.Add("ExitOnForwardFailure=yes");

        parts.Add("-o");
        parts.Add("ServerAliveInterval=30");
        parts.Add("-o");
        parts.Add("ServerAliveCountMax=3");

        parts.Add($"{p.SshUser}@{p.SshHost}");

        return string.Join(" ", parts.Select(QuoteIfNeeded));
    }

    private static string ExpandHome(string path)
    {
        if (path.StartsWith("~"))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(
                home,
                path.TrimStart('~')
                    .TrimStart('\\', '/')
            );
        }
        return path;
    }

    private static string QuoteIfNeeded(string arg)
        => arg.Contains(' ') ? $"\"{arg}\"" : arg;

    private static string ToStrictHostKeyChecking(SshStrictHostKeyChecking mode)
    {
        return mode switch
        {
            SshStrictHostKeyChecking.Yes => "yes",
            SshStrictHostKeyChecking.No => "no",
            SshStrictHostKeyChecking.AcceptNew => "accept-new",
            _ => "yes"
        };
    }
}
