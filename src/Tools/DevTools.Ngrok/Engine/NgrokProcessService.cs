using System.Diagnostics;
using DevTools.Ngrok.Models;

namespace DevTools.Ngrok.Engine;

public sealed class NgrokProcessService
{
    public int? StartHttp(NgrokStartOptions options)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        var proto = string.IsNullOrWhiteSpace(options.Protocol) ? "http" : options.Protocol;
        var target = proto.Equals("https", StringComparison.OrdinalIgnoreCase)
            ? $"https://localhost:{options.Port}"
            : $"http://localhost:{options.Port}";

        var fileName = string.IsNullOrWhiteSpace(options.ExecutablePath) ? "ngrok" : options.ExecutablePath;
        var args = new List<string> { "http", target };

        if (options.ExtraArgs is { Count: > 0 })
            args.AddRange(options.ExtraArgs);

        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = string.Join(" ", args.Select(QuoteIfNeeded)),
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var proc = Process.Start(psi);
        return proc?.Id;
    }

    public int KillAll(string processName = "ngrok")
    {
        var procs = Process.GetProcessesByName(processName);
        var killed = 0;

        foreach (var p in procs)
        {
            try { p.Kill(); killed++; } catch { }
        }

        return killed;
    }

    public bool HasAny(string processName = "ngrok")
        => Process.GetProcessesByName(processName).Length > 0;

    private static string QuoteIfNeeded(string arg)
        => arg.Contains(' ') ? $"\"{arg}\"" : arg;
}
