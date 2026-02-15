using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DevTools.Core.Results;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DevTools.Presentation.Wpf.Components;

public partial class RunSummaryControl : System.Windows.Controls.UserControl, INotifyPropertyChanged
{
    public RunSummaryControl()
    {
        InitializeComponent();
        DataContext = this;
    }

    private RunResult? _result;
    
    public void BindResult(RunResult result)
    {
        _result = result;
        OnPropertyChanged(string.Empty); // Refresh all bindings
    }

    public void Clear()
    {
        _result = null;
        OnPropertyChanged(string.Empty);
    }

    public bool HasResult => _result != null;
    
    public string StatusIcon => _result?.IsSuccess == true ? "✅" : "❌";
    
    public string StatusText => _result?.IsSuccess == true ? "Execução Concluída" : "Falha na Execução";
    
    public System.Windows.Media.Brush StatusColor => _result?.IsSuccess == true 
        ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(92, 184, 92)) // Green
        : new SolidColorBrush(System.Windows.Media.Color.FromRgb(217, 83, 79)); // Red

    public string Duration => _result?.Summary.Duration.ToString(@"hh\:mm\:ss") ?? "";
    
    public int ProcessedCount => _result?.Summary.Processed ?? 0;
    public int ChangedCount => _result?.Summary.Changed ?? 0;
    public int IgnoredCount => _result?.Summary.Ignored ?? 0;
    public int FailedCount => _result?.Summary.Failed ?? 0;
    
    public System.Windows.Media.Brush FailedColor => FailedCount > 0 
        ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(217, 83, 79)) // Red
        : new SolidColorBrush(System.Windows.Media.Colors.White);

    public bool HasErrors => _result?.Errors.Count > 0;
    
    public IEnumerable<ErrorDetail>? Errors => _result?.Errors;
    
    public string? OutputLocation => _result?.Summary.OutputLocation;

    public event PropertyChangedEventHandler? PropertyChanged;
    
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
