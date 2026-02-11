using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using DevTools.Presentation.Wpf.Services;
using DevTools.Snapshot.Engine;
using DevTools.Snapshot.Models;
using Microsoft.Win32;

namespace DevTools.Presentation.Wpf.Views;

public partial class SnapshotWindow : Window
{
    private readonly JobManager _jobManager;
    private readonly SettingsService _settingsService;

    public SnapshotWindow(JobManager jobManager, SettingsService settingsService)
    {
        InitializeComponent();
        _jobManager = jobManager;
        _settingsService = settingsService;

        if (!string.IsNullOrEmpty(_settingsService.Settings.LastSnapshotRootPath))
            RootPathBox.Text = _settingsService.Settings.LastSnapshotRootPath;

        /* Position handled by TrayService
        if (_settingsService.Settings.SnapshotWindowTop.HasValue)
        {
            Top = _settingsService.Settings.SnapshotWindowTop.Value;
            Left = _settingsService.Settings.SnapshotWindowLeft.Value;
        }
        else
        {
            var screen = SystemParameters.WorkArea;
            Left = screen.Right - Width - 20;
            Top = screen.Bottom - Height - 20;
        }

        var workArea = SystemParameters.WorkArea;
        if (Top < 0 || Top > workArea.Height) Top = workArea.Height - Height - 20;
        if (Left < 0 || Left > workArea.Width) Left = workArea.Width - Width - 20;
        */

        /*
        Closed += (s, e) =>
        {
            _settingsService.Settings.SnapshotWindowTop = Top;
            _settingsService.Settings.SnapshotWindowLeft = Left;
            _settingsService.Save();
        };
        */
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // if (e.ButtonState == MouseButtonState.Pressed) DragMove();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void BrowseRoot_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog { Title = "Selecione a Pasta do Projeto" };
        if (dlg.ShowDialog() == true)
        {
            RootPathBox.Text = dlg.FolderName;
            _settingsService.Settings.LastSnapshotRootPath = dlg.FolderName;
            _settingsService.Save();
        }
    }

    private void ProcessButton_Click(object sender, RoutedEventArgs e)
    {
        var root = RootPathBox.Text;
        if (string.IsNullOrWhiteSpace(root))
        {
            MessageBox.Show("Pasta do Projeto é obrigatória.", "Dados Incompletos", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var genText = TextCheck.IsChecked ?? true;
        var genHtml = HtmlCheck.IsChecked ?? false;
        var genJsonNested = JsonNestedCheck.IsChecked ?? false;
        var genJsonRecursive = JsonRecursiveCheck.IsChecked ?? false;

        Close();

        _jobManager.StartJob("Snapshot", async (reporter, ct) =>
        {
            var engine = new SnapshotEngine();
            var request = new SnapshotRequest(
                RootPath: root,
                GenerateText: genText,
                GenerateHtmlPreview: genHtml,
                GenerateJsonNested: genJsonNested,
                GenerateJsonRecursive: genJsonRecursive
            );

            var result = await engine.ExecuteAsync(request, reporter, ct);

            return result.IsSuccess
                ? $"Snapshot gerado com sucesso na pasta 'Snapshot'!"
                : $"Falha ao gerar Snapshot: {string.Join(", ", result.Errors.Select(x => x.Message))}";
        });
    }
}
