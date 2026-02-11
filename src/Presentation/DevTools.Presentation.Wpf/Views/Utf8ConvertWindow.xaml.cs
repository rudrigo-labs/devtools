using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using DevTools.Presentation.Wpf.Services;
using DevTools.Utf8Convert.Engine;
using DevTools.Utf8Convert.Models;
using Microsoft.Win32;

namespace DevTools.Presentation.Wpf.Views;

public partial class Utf8ConvertWindow : Window
{
    private readonly JobManager _jobManager;
    private readonly SettingsService _settingsService;

    public Utf8ConvertWindow(JobManager jobManager, SettingsService settingsService)
    {
        InitializeComponent();
        _jobManager = jobManager;
        _settingsService = settingsService;

        if (!string.IsNullOrEmpty(_settingsService.Settings.LastUtf8RootPath))
            RootPathBox.Text = _settingsService.Settings.LastUtf8RootPath;

        /* Position handled by TrayService
        if (_settingsService.Settings.Utf8WindowTop.HasValue)
        {
            Top = _settingsService.Settings.Utf8WindowTop.Value;
            Left = _settingsService.Settings.Utf8WindowLeft.Value;
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
            _settingsService.Settings.Utf8WindowTop = Top;
            _settingsService.Settings.Utf8WindowLeft = Left;
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
        var dlg = new OpenFolderDialog { Title = "Selecione a Pasta Raiz" };
        if (dlg.ShowDialog() == true)
        {
            RootPathBox.Text = dlg.FolderName;
            _settingsService.Settings.LastUtf8RootPath = dlg.FolderName;
            _settingsService.Save();
        }
    }

    private void ProcessButton_Click(object sender, RoutedEventArgs e)
    {
        var root = RootPathBox.Text;
        if (string.IsNullOrWhiteSpace(root))
        {
            MessageBox.Show("Pasta Raiz é obrigatória.", "Dados Incompletos", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var recursive = RecursiveCheck.IsChecked ?? true;
        var backup = BackupCheck.IsChecked ?? true;
        var bom = BomCheck.IsChecked ?? true;

        Close();

        _jobManager.StartJob("Utf8Convert", async (reporter, ct) =>
        {
            var engine = new Utf8ConvertEngine();
            var request = new Utf8ConvertRequest(
                RootPath: root,
                Recursive: recursive,
                CreateBackup: backup,
                OutputBom: bom
            );

            var result = await engine.ExecuteAsync(request, reporter, ct);

            return result.IsSuccess
                ? $"Conversão concluída! {result.Value?.Summary.Converted ?? 0} arquivos convertidos."
                : $"Falha na conversão: {string.Join(", ", result.Errors.Select(x => x.Message))}";
        });
    }
}
