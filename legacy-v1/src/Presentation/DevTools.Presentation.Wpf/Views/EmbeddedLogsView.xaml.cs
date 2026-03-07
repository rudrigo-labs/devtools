using System;
using System.IO;
using System.Windows;
using DevTools.Presentation.Wpf.Services;
using WpfControls = System.Windows.Controls;

namespace DevTools.Presentation.Wpf.Views;

public partial class EmbeddedLogsView : WpfControls.UserControl
{
    public EmbeddedLogsView()
    {
        InitializeComponent();
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
                using var fs = new FileStream(AppLogger.LogFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var sr = new StreamReader(fs);
                LogTextBox.Text = sr.ReadToEnd();
                LogTextBox.ScrollToEnd();
            }
            else
            {
                LogTextBox.Text = "Arquivo de log nao encontrado.";
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
            File.WriteAllText(AppLogger.LogFilePath, string.Empty);
            RefreshLogs();
        }
        catch (Exception ex)
        {
            UiMessageService.ShowError("Nao foi possivel limpar o arquivo de log.", "Erro ao limpar log", ex);
        }
    }

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var directory = Path.GetDirectoryName(AppLogger.LogFilePath);
            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = directory,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            UiMessageService.ShowError("Erro ao abrir pasta de logs.", "Erro ao abrir pasta", ex);
        }
    }
}
