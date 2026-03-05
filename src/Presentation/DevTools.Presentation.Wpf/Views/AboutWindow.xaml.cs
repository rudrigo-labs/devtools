using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using DevTools.Presentation.Wpf.Services;

namespace DevTools.Presentation.Wpf.Views;

public partial class AboutWindow : Window
{
    private const string RepositoryUrl = "https://rudrigo-labs.github.io/devtools/";

    public AboutWindow()
    {
        InitializeComponent();
        LoadVersion();
    }

    private void LoadVersion()
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version;
        if (version != null)
        {
            VersionText.Text = $"Versao: {version.Major}.{version.Minor}.{version.Build}";
        }
    }

    private void OpenGitHubButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = RepositoryUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            UiMessageService.ShowError("Nao foi possivel abrir o site do DevTools.", "Erro", ex);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
