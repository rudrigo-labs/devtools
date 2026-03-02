using System.Diagnostics;
using System.IO;
using System.Windows;
using DevTools.Presentation.Wpf.Services;
using DevTools.Presentation.Wpf.Utilities;

namespace DevTools.Presentation.Wpf.Views;

public partial class LogsWindow : Window
{
    private string? _logFilePath;

    public LogsWindow()
    {
        InitializeComponent();
        _logFilePath = AppLogger.LogFilePath;
        LoadLogs();
    }

    private void LoadLogs()
    {
        if (string.IsNullOrEmpty(_logFilePath) || !File.Exists(_logFilePath))
        {
            LogBox.Text = "Nenhum arquivo de log encontrado.";
            return;
        }

        try
        {
            using (var fs = new FileStream(_logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(fs))
            {
                LogBox.Text = reader.ReadToEnd();
                LogBox.ScrollToEnd();
            }
        }
        catch (Exception ex)
        {
            LogBox.Text = $"Erro ao ler log: {ex.Message}";
        }
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        LoadLogs();
    }

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_logFilePath)) return;
        
        var dir = Path.GetDirectoryName(_logFilePath);
        if (Directory.Exists(dir))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = dir,
                UseShellExecute = true,
                Verb = "open"
            });
        }
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_logFilePath) || !File.Exists(_logFilePath)) return;

        try
        {
            File.WriteAllText(_logFilePath, string.Empty);
            LoadLogs();
        }
        catch (Exception ex)
        {
            DevToolsMessage.Error($"Erro ao limpar logs: {ex.Message}", "Erro");
        }
    }

    private void Header_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        DragMove();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
