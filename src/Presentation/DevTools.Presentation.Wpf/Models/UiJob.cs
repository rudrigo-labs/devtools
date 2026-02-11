using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DevTools.Presentation.Wpf.Models;

public enum UiJobStatus
{
    Pending = 0,
    Running = 1,
    Success = 2,
    Error = 3,
    Canceled = 4
}

public sealed class UiJob : INotifyPropertyChanged
{
    private string _name = "";
    private UiJobStatus _status = UiJobStatus.Pending;
    private int _progressPercent;
    private string _message = "";
    private DateTimeOffset _startedAt = DateTimeOffset.Now;
    private DateTimeOffset? _completedAt;
    private string _logs = "";

    public Guid Id { get; set; }

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    public string Logs
    {
        get => _logs;
        set { _logs = value; OnPropertyChanged(); }
    }

    public UiJobStatus Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); }
    }

    public int ProgressPercent
    {
        get => _progressPercent;
        set { _progressPercent = value; OnPropertyChanged(); }
    }

    public string Message
    {
        get => _message;
        set { _message = value; OnPropertyChanged(); }
    }

    public DateTimeOffset StartedAt
    {
        get => _startedAt;
        set { _startedAt = value; OnPropertyChanged(); }
    }

    public DateTimeOffset? CompletedAt
    {
        get => _completedAt;
        set { _completedAt = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
