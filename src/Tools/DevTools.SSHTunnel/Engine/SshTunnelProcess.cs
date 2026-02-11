using DevTools.Core.Abstractions;
using DevTools.Core.Models;
using DevTools.SSHTunnel.Abstractions;

namespace DevTools.SSHTunnel.Engine;

public sealed class SshTunnelProcess : IDisposable
{
    private readonly IProcessRunner _runner;
    private CancellationTokenSource? _cts;
    private Task<ProcessResult>? _runTask;
    private ProcessResult? _lastResult;
    private Exception? _lastException;
    private int? _processId;

    public SshTunnelProcess(IProcessRunner runner)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
    }

    public bool IsRunning => _runTask is { IsCompleted: false };

    public int? ProcessId => _processId;

    public ProcessResult? LastResult => _lastResult;

    public Exception? LastException => _lastException;

    public async Task StartAsync(string args, TimeSpan startupTimeout, CancellationToken ct = default)
    {
        if (IsRunning)
            return;

        _cts?.Dispose();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        _runTask = _runner.RunAsync("ssh", args, null, null, _cts.Token);

        if (_runner is IProcessRunnerWithPid pidRunner)
            _processId = pidRunner.LastProcessId;

        _ = _runTask.ContinueWith(t =>
        {
            if (t.Status == TaskStatus.RanToCompletion)
                _lastResult = t.Result;
            else if (t.Exception is not null)
                _lastException = t.Exception.GetBaseException();
        }, TaskScheduler.Default);

        var completed = await Task.WhenAny(_runTask, Task.Delay(startupTimeout, ct)).ConfigureAwait(false);

        if (completed == _runTask)
        {
            try
            {
                var result = await _runTask.ConfigureAwait(false);
                _lastResult = result;
                throw new SshTunnelConnectionException(
                    "sshtunnel.connection.failed",
                    "SSH encerrou antes de estabelecer o túnel.",
                    BuildDetails(result));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SshTunnelConnectionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _lastException = ex;
                throw new SshTunnelConnectionException(
                    "sshtunnel.connection.failed",
                    "Falha ao iniciar o SSH.",
                    ex.Message,
                    ex);
            }
        }
    }

    public async Task StopAsync(TimeSpan timeout, CancellationToken ct = default)
    {
        if (_cts is null || _runTask is null)
            return;

        try
        {
            _cts.Cancel();
        }
        catch
        {
            // ignore
        }

        try
        {
            await Task.WhenAny(_runTask, Task.Delay(timeout, ct)).ConfigureAwait(false);

            if (!_runTask.IsCompleted)
                TryKillByPid();
        }
        catch (OperationCanceledException)
        {
            // ignore
        }
        catch
        {
            // ignore
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
        }
    }

    private void TryKillByPid()
    {
        if (!_processId.HasValue)
            return;

        try
        {
            using var process = System.Diagnostics.Process.GetProcessById(_processId.Value);
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(1500);
            }
        }
        catch
        {
            // ignore
        }
    }

    private static string BuildDetails(ProcessResult result)
    {
        var details = new List<string>();
        if (!string.IsNullOrWhiteSpace(result.StdErr))
            details.Add($"STDERR:\n{result.StdErr.Trim()}");
        if (!string.IsNullOrWhiteSpace(result.StdOut))
            details.Add($"STDOUT:\n{result.StdOut.Trim()}");
        return details.Count == 0
            ? $"ExitCode: {result.ExitCode}"
            : string.Join("\n\n", details);
    }

    public void Dispose()
    {
        try
        {
            StopAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();
        }
        catch
        {
            // ignore
        }
    }
}
