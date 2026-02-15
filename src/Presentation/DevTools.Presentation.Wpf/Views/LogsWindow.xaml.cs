using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using DevTools.Presentation.Wpf.Services;

namespace DevTools.Presentation.Wpf.Views;

public partial class LogsWindow : Window
{
    private readonly SettingsService _settingsService;

    public LogsWindow(SettingsService settingsService)
    {
        InitializeComponent();
        _settingsService = settingsService;
        
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        RefreshLogs();
    }

    private void RefreshLogs()
    {
        try
        {
            if (File.Exists(AppLogger.LogFilePath))
            {
                // Read with file share to avoid locking issues if app is writing
                using var fs = new FileStream(AppLogger.LogFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var sr = new StreamReader(fs);
                LogTextBox.Text = sr.ReadToEnd();
                LogTextBox.ScrollToEnd();
            }
            else
            {
                LogTextBox.Text = "Arquivo de log não encontrado.";
            }
        }
        catch (Exception ex)
        {
            LogTextBox.Text = $"Erro ao ler logs: {ex.Message}";
        }
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        RefreshLogs();
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Try to write empty
            File.WriteAllText(AppLogger.LogFilePath, string.Empty);
            RefreshLogs();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Não foi possível limpar o arquivo de log: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dir = Path.GetDirectoryName(AppLogger.LogFilePath);
            if (dir != null && Directory.Exists(dir))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = dir,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Erro ao abrir pasta: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
