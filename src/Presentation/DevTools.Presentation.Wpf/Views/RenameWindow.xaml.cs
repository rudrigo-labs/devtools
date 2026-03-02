using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using DevTools.Presentation.Wpf.Services;
using DevTools.Rename.Engine;
using DevTools.Rename.Models;
using System.Collections.Generic;
using DevTools.Core.Models;

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
            RootPathSelector.SelectedPath = _settingsService.Settings.LastRenameRootPath;

        if (!string.IsNullOrEmpty(_settingsService.Settings.LastRenameInclude))
            IncludeBox.Text = _settingsService.Settings.LastRenameInclude;
            
        if (!string.IsNullOrEmpty(_settingsService.Settings.LastRenameExclude))
            ExcludeBox.Text = _settingsService.Settings.LastRenameExclude;

        if (_settingsService.Settings.LastRenameDryRun.HasValue)
            DryRunCheck.IsChecked = _settingsService.Settings.LastRenameDryRun.Value;

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

    private void BrowseRoot_Click(object sender, RoutedEventArgs e) { }

    private async void ProcessButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateInputs(out var errorMessage))
        {
            UiMessageService.ShowError(errorMessage, "Erro de Validação");
            return;
        }

        var root = RootPathSelector.SelectedPath;
        var oldText = OldTextBox.Text;
        var newText = NewTextBox.Text;
        var mode = ModeCombo.SelectedIndex == 1 ? RenameMode.NamespaceOnly : RenameMode.General;
        var backup = BackupCheck.IsChecked ?? true;
        var undo = UndoLogCheck.IsChecked ?? true;
        var dryRun = DryRunCheck.IsChecked ?? false;
        var include = IncludeBox.Text;
        var exclude = ExcludeBox.Text;

        if (RememberSettingsCheck.IsChecked == true)
        {
            _settingsService.Settings.LastRenameRootPath = root;
            _settingsService.Settings.LastRenameInclude = include;
            _settingsService.Settings.LastRenameExclude = exclude;
            _settingsService.Settings.LastRenameDryRun = dryRun;
            _settingsService.Save();
        }

        var request = new RenameRequest(
            RootPath: root,
            OldText: oldText,
            NewText: newText ?? "",
            Mode: mode,
            DryRun: dryRun,
            IncludeGlobs: string.IsNullOrWhiteSpace(include) ? null : include.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            ExcludeGlobs: string.IsNullOrWhiteSpace(exclude) ? null : exclude.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            BackupEnabled: backup,
            WriteUndoLog: undo
        );

        // Execute directly to show result in-place
        var engine = new RenameEngine();
        
        // Disable UI while running
        IsEnabled = false;
        RunSummary.Clear();
        
        try 
        {
            var result = await System.Threading.Tasks.Task.Run(() => engine.ExecuteAsync(request));
            RunSummary.BindResult(result);
        }
        catch (Exception ex)
        {
            UiMessageService.ShowError("Erro crítico ao executar renomeação.", "Erro na ferramenta Rename", ex);
        }
        finally
        {
            IsEnabled = true;
        }
    }

    private bool ValidateInputs(out string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(RootPathSelector.SelectedPath))
        {
            errorMessage = "Pasta Raiz é obrigatória.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(OldTextBox.Text))
        {
            errorMessage = "Texto Antigo é obrigatório.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }
}
