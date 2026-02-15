using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using DevTools.Core.Abstractions;
using DevTools.Core.Models;
using DevTools.Presentation.Wpf.Models;

namespace DevTools.Presentation.Wpf.Services;

public sealed class JobManager
{
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _ctsByJobId = new();
    private readonly SynchronizationContext _uiContext;

    public ObservableCollection<UiJob> Jobs { get; } = new();

    public event Action<string, bool>? OnJobCompleted;

    public JobManager()
    {
        // Captura o contexto de UI (ideal: instanciar JobManager no startup do WPF).
        _uiContext = SynchronizationContext.Current ?? new SynchronizationContext();
    }

    public Guid StartJob(
        string jobName,
        Func<IProgressReporter, CancellationToken, Task<string>> action)
    {
        var jobId = Guid.NewGuid();
        var cts = new CancellationTokenSource();

        var job = new UiJob
        {
            Id = jobId,
            Name = jobName,
            Status = UiJobStatus.Running,
            ProgressPercent = 0,
            Message = "Iniciando...",
            StartedAt = DateTimeOffset.Now
        };

        _ctsByJobId[jobId] = cts;

        RunOnUi(() => Jobs.Insert(0, job));

        var reporter = new UiProgressReporter(this, jobId);

        _ = Task.Run(async () =>
        {
            try
            {
                // exec
                var resultMessage = await action(reporter, cts.Token).ConfigureAwait(false);

                Complete(jobId, success: true, message: resultMessage);
            }
            catch (OperationCanceledException)
            {
                Complete(jobId, success: false, message: "Cancelado.", canceled: true);
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Job '{jobName}' failed", ex);
                Complete(jobId, success: false, message: $"{jobName} falhou: {ex.Message}");
            }
            finally
            {
                _ctsByJobId.TryRemove(jobId, out _);
            }
        }, cts.Token);

        return jobId;
    }

    public bool CancelJob(Guid jobId)
    {
        if (_ctsByJobId.TryGetValue(jobId, out var cts))
        {
            cts.Cancel();
            return true;
        }

        return false;
    }

    public UiJob? GetJob(Guid jobId)
        => Jobs.FirstOrDefault(j => j.Id == jobId);

    internal void OnProgress(Guid jobId, ProgressEvent ev)
    {
        var job = GetJob(jobId);
        if (job is null) return;

        RunOnUi(() =>
        {
            // message
            if (!string.IsNullOrWhiteSpace(ev.Message))
            {
                job.Message = ev.Message;
                // Append ao log acumulado
                var timestamp = DateTime.Now.ToString("HH:mm:ss");
                job.Logs += $"[{timestamp}] {ev.Message}{Environment.NewLine}";
            }

            // percent (no seu Core é Percent, não Percentage)
            if (ev.Percent.HasValue)
                job.ProgressPercent = Clamp0To100(ev.Percent.Value);

            // mantém Running enquanto reporta
            if (job.Status == UiJobStatus.Running)
                job.Status = UiJobStatus.Running;
        });
    }

    private void Complete(Guid jobId, bool success, string message, bool canceled = false)
    {
        var job = GetJob(jobId);
        if (job is null) return;

        RunOnUi(() =>
        {
            job.CompletedAt = DateTimeOffset.Now;
            job.Message = message;
            
            // Log final
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            job.Logs += $"[{timestamp}] Finalizado: {message}{Environment.NewLine}";

            if (canceled)
            {
                job.Status = UiJobStatus.Canceled;
                return;
            }

            job.Status = success ? UiJobStatus.Success : UiJobStatus.Error;

            // se concluiu com sucesso e nunca recebeu percent, fecha em 100
            if (success && job.ProgressPercent < 100)
                job.ProgressPercent = 100;

            // Notifica assinantes (ex: Tray)
            OnJobCompleted?.Invoke(message, success);
        });
    }

    private void RunOnUi(Action action)
    {
        // Se já estiver na UI thread, roda direto
        if (System.Windows.Application.Current?.Dispatcher?.CheckAccess() == true)
        {
            action();
            return;
        }

        // senão, posta no contexto capturado
        _uiContext.Post(_ => action(), null);
    }

    private static int Clamp0To100(int value)
        => value < 0 ? 0 : value > 100 ? 100 : value;

    private sealed class UiProgressReporter : IProgressReporter
    {
        private readonly JobManager _jobManager;
        private readonly Guid _jobId;

        public UiProgressReporter(JobManager jobManager, Guid jobId)
        {
            _jobManager = jobManager;
            _jobId = jobId;
        }

        public void Report(ProgressEvent ev)
        {
            _jobManager.OnProgress(_jobId, ev);
        }
    }
}
