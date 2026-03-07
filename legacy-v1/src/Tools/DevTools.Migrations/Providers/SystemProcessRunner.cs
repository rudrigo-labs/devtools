using System.Diagnostics;
using System.Text;
using DevTools.Core.Abstractions;
using DevTools.Core.Models;

namespace DevTools.Migrations.Providers;

public sealed class SystemProcessRunner : IProcessRunner
{
    public async Task<ProcessResult> RunAsync(
        string fileName,
        string arguments,
        string? workingDirectory = null,
        IDictionary<string, string?>? environment = null,
        CancellationToken ct = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = workingDirectory ?? string.Empty
        };

        if (environment is not null)
        {
            foreach (var kv in environment)
                psi.Environment[kv.Key] = kv.Value;
        }

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        process.OutputDataReceived += (_, e) => { if (e.Data is not null) stdout.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) stderr.AppendLine(e.Data); };

        var started = process.Start();
        if (!started)
            throw new InvalidOperationException($"Failed to start process '{fileName}'.");

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var sw = Stopwatch.StartNew();

        try
        {
            await process.WaitForExitAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            sw.Stop();
        }

        return new ProcessResult(process.ExitCode, stdout.ToString(), stderr.ToString(), sw.Elapsed);
    }
}
