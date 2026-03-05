using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using DevTools.Presentation.Wpf.Services;

namespace DevTools.Presentation.Wpf.Views;

public partial class AboutWindow : Window
{
    private const string RepositoryUrl = "https://github.com/rudrigo-labs/devtools";

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
            VersionText.Text = $"Versão: {version.Major}.{version.Minor}.{version.Build}";
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
            UiMessageService.ShowError("Não foi possível abrir o link do repositório.", "Erro", ex);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
