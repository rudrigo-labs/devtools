using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using DevTools.Presentation.Wpf.Services;
using DevTools.Rename.Engine;
using DevTools.Rename.Models;
using Microsoft.Win32;

namespace DevTools.Presentation.Wpf.Views;

public partial class RenameWindow : Window
{
    private readonly JobManager _jobManager;
    private readonly SettingsService _settingsService;

    public RenameWindow(JobManager jobManager, SettingsService settingsService)
    {
        InitializeComponent();
        _jobManager = jobManager;
        _settingsService = settingsService;

        if (!string.IsNullOrEmpty(_settingsService.Settings.LastRenameRootPath))
            RootPathBox.Text = _settingsService.Settings.LastRenameRootPath;

        /* Position handled by TrayService
        if (_settingsService.Settings.RenameWindowTop.HasValue)
        {
            Top = _settingsService.Settings.RenameWindowTop.Value;
            Left = _settingsService.Settings.RenameWindowLeft.Value;
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
            _settingsService.Settings.RenameWindowTop = Top;
            _settingsService.Settings.RenameWindowLeft = Left;
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
            _settingsService.Settings.LastRenameRootPath = dlg.FolderName;
            _settingsService.Save();
        }
    }

    private void ProcessButton_Click(object sender, RoutedEventArgs e)
    {
        var root = RootPathBox.Text;
        var oldText = OldTextBox.Text;
        var newText = NewTextBox.Text;

        if (string.IsNullOrWhiteSpace(root) || string.IsNullOrEmpty(oldText))
        {
            MessageBox.Show("Pasta Raiz e Texto Antigo são obrigatórios.", "Dados Incompletos", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var mode = ModeCombo.SelectedIndex == 1 ? RenameMode.NamespaceOnly : RenameMode.General;
        var backup = BackupCheck.IsChecked ?? true;
        var undo = UndoLogCheck.IsChecked ?? true;

        Close();

        _jobManager.StartJob("Rename", async (reporter, ct) =>
        {
            var engine = new RenameEngine();
            var request = new RenameRequest(
                RootPath: root,
                OldText: oldText,
                NewText: newText ?? "",
                Mode: mode,
                BackupEnabled: backup,
                WriteUndoLog: undo
            );

            var result = await engine.ExecuteAsync(request, reporter, ct);

            return result.IsSuccess
                ? $"Renomeação concluída! {result.Value?.Summary.FilesRenamed ?? 0} arquivos alterados."
                : $"Falha na renomeação: {string.Join(", ", result.Errors.Select(x => x.Message))}";
        });
    }
}
