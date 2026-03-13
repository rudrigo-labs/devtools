using System.Diagnostics;
using System.Text;
using DevTools.Core.Abstractions;
using DevTools.Core.Models;
using DevTools.SSHTunnel.Abstractions;

namespace DevTools.SSHTunnel.Providers;

/// <summary>
/// Wrapper de IProcessRunner que implementa IProcessRunnerWithPid.
/// Necessário para capturar o PID do processo SSH em andamento.
/// </summary>
public sealed class SshProcessRunner : IProcessRunnerWithPid
{
    private readonly IProcessRunner? _inner;

    public int? LastProcessId { get; private set; }

    /// <summary>
    /// Construtor sem dependência — cria processo diretamente.
    /// </summary>
    public SshProcessRunner() { }

    /// <summary>
    /// Construtor com IProcessRunner externo — wrapping transparente com captura de PID.
    /// </summary>
    public SshProcessRunner(IProcessRunner inner)
    {
        _inner = inner;
    }

    public async Task<ProcessResult> RunAsync(
        string fileName,
        string arguments,
        string? workingDirectory = null,
        IDictionary<string, string?>? environment = null,
        CancellationToken cancellationToken = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName             = fileName,
            Arguments            = arguments,
            UseShellExecute      = false,
            CreateNoWindow       = true,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            WorkingDirectory     = workingDirectory ?? string.Empty
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
        process.ErrorDataReceived  += (_, e) => { if (e.Data is not null) stderr.AppendLine(e.Data); };

        var started = process.Start();
        if (!started)
            throw new InvalidOperationException($"Falha ao iniciar processo '{fileName}'.");

        LastProcessId = process.Id;

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var sw = Stopwatch.StartNew();
        try
        {
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            throw;
        }
        finally
        {
            sw.Stop();
        }

        return new ProcessResult(process.ExitCode, stdout.ToString(), stderr.ToString(), sw.Elapsed);
    }

    private static void TryKill(Process process)
    {
        try { if (!process.HasExited) { process.Kill(entireProcessTree: true); process.WaitForExit(1500); } }
        catch { /* ignore */ }
    }
}
