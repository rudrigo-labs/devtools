using System;
using System.Windows;
using System.Windows.Input;
using DevTools.Core.Configuration;
using DevTools.Presentation.Wpf.Services;
using DevTools.Rename.Engine;
using DevTools.Rename.Models;

namespace DevTools.Presentation.Wpf.Views;

public partial class RenameWindow : Window
{
    private readonly JobManager _jobManager = null!;
    private readonly SettingsService _settingsService = null!;
    private readonly ToolConfigurationManager _toolConfigurationManager = null!;

    public RenameWindow(JobManager jobManager, SettingsService settingsService, ToolConfigurationManager toolConfigurationManager)
    {
        InitializeComponent();
        _jobManager = jobManager;
        _settingsService = settingsService;
        _toolConfigurationManager = toolConfigurationManager;

        Loaded += OnLoaded;
    }

    // Construtor para o Designer
    public RenameWindow()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplyDefaultConfiguration();

        if (!string.IsNullOrWhiteSpace(_settingsService.Settings.LastRenameRootPath))
            RootPathSelector.SelectedPath = _settingsService.Settings.LastRenameRootPath;
        if (!string.IsNullOrWhiteSpace(_settingsService.Settings.LastRenameInclude))
            IncludeBox.Text = _settingsService.Settings.LastRenameInclude;
        if (!string.IsNullOrWhiteSpace(_settingsService.Settings.LastRenameExclude))
            ExcludeBox.Text = _settingsService.Settings.LastRenameExclude;
        if (_settingsService.Settings.LastRenameDryRun.HasValue)
            DryRunCheck.IsChecked = _settingsService.Settings.LastRenameDryRun.Value;
        if (!string.IsNullOrWhiteSpace(_settingsService.Settings.LastRenameUndoLogPath))
            UndoLogPathInput.Text = _settingsService.Settings.LastRenameUndoLogPath;
        if (!string.IsNullOrWhiteSpace(_settingsService.Settings.LastRenameReportPath))
            ReportPathInput.Text = _settingsService.Settings.LastRenameReportPath;
        MaxDiffLinesInput.Text = (_settingsService.Settings.LastRenameMaxDiffLinesPerFile ?? 200).ToString();
    }

    private void ApplyDefaultConfiguration()
    {
        var configuration = _toolConfigurationManager.GetDefaultConfiguration("Rename");
        if (configuration == null)
            return;

        if (configuration.Options.TryGetValue("root-path", out var rootPath) && !string.IsNullOrWhiteSpace(rootPath))
            RootPathSelector.SelectedPath = rootPath;

        if (configuration.Options.TryGetValue("old-text", out var oldText))
            OldTextBox.Text = oldText ?? string.Empty;

        if (configuration.Options.TryGetValue("new-text", out var newText))
            NewTextBox.Text = newText ?? string.Empty;

        if (configuration.Options.TryGetValue("include", out var include))
            IncludeBox.Text = include ?? string.Empty;

        if (configuration.Options.TryGetValue("exclude", out var exclude))
            ExcludeBox.Text = exclude ?? string.Empty;

        if (configuration.Options.TryGetValue("mode", out var mode))
            ModeCombo.SelectedIndex = string.Equals(mode, "namespace", StringComparison.OrdinalIgnoreCase) ? 1 : 0;

        if (configuration.Options.TryGetValue("backup-enabled", out var backupEnabled) && bool.TryParse(backupEnabled, out var parsedBackup))
            BackupCheck.IsChecked = parsedBackup;

        if (configuration.Options.TryGetValue("write-undo-log", out var writeUndoLog) && bool.TryParse(writeUndoLog, out var parsedUndoLog))
            UndoLogCheck.IsChecked = parsedUndoLog;

        if (configuration.Options.TryGetValue("dry-run", out var dryRun) && bool.TryParse(dryRun, out var parsedDryRun))
            DryRunCheck.IsChecked = parsedDryRun;

        if (configuration.Options.TryGetValue("undo-log-path", out var undoLogPath))
            UndoLogPathInput.Text = undoLogPath ?? string.Empty;

        if (configuration.Options.TryGetValue("report-path", out var reportPath))
            ReportPathInput.Text = reportPath ?? string.Empty;

        if (configuration.Options.TryGetValue("max-diff-lines-per-file", out var maxDiffLines) && !string.IsNullOrWhiteSpace(maxDiffLines))
            MaxDiffLinesInput.Text = maxDiffLines;
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
            ValidationUiService.ShowInline(MainFrame, errorMessage);
            return;
        }

        ValidationUiService.ClearInline(MainFrame);

        string root = RootPathSelector.SelectedPath ?? string.Empty;
        string oldText = OldTextBox.Text ?? string.Empty;
        string newText = NewTextBox.Text ?? string.Empty;
        var mode = ModeCombo.SelectedIndex == 1 ? RenameMode.NamespaceOnly : RenameMode.General;
        var backup = BackupCheck.IsChecked ?? true;
        var undo = UndoLogCheck.IsChecked ?? true;
        var dryRun = DryRunCheck.IsChecked ?? false;
        var include = IncludeBox.Text;
        var exclude = ExcludeBox.Text;
        var undoLogPath = UndoLogPathInput.Text;
        var reportPath = ReportPathInput.Text;
        var maxDiffLines = int.TryParse(MaxDiffLinesInput.Text, out var parsedMaxDiff) ? parsedMaxDiff : 200;

        if (RememberSettingsCheck.IsChecked == true)
        {
            _settingsService.Settings.LastRenameRootPath = root;
            _settingsService.Settings.LastRenameInclude = include;
            _settingsService.Settings.LastRenameExclude = exclude;
            _settingsService.Settings.LastRenameDryRun = dryRun;
            _settingsService.Settings.LastRenameUndoLogPath = undoLogPath;
            _settingsService.Settings.LastRenameReportPath = reportPath;
            _settingsService.Settings.LastRenameMaxDiffLinesPerFile = maxDiffLines;
            _settingsService.Save();
        }

        var request = new RenameRequest(
            RootPath: root,
            OldText: oldText,
            NewText: newText,
            Mode: mode,
            DryRun: dryRun,
            IncludeGlobs: string.IsNullOrWhiteSpace(include) ? null : include.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            ExcludeGlobs: string.IsNullOrWhiteSpace(exclude) ? null : exclude.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            BackupEnabled: backup,
            WriteUndoLog: undo,
            UndoLogPath: string.IsNullOrWhiteSpace(undoLogPath) ? null : undoLogPath.Trim(),
            ReportPath: string.IsNullOrWhiteSpace(reportPath) ? null : reportPath.Trim(),
            MaxDiffLinesPerFile: maxDiffLines
        );

        var engine = new RenameEngine();

        IsEnabled = false;
        RunSummary.Clear();

        try
        {
            var result = await System.Threading.Tasks.Task.Run(() => engine.ExecuteAsync(request));
            RunSummary.BindResult(result);
        }
        catch (Exception ex)
        {
            UiMessageService.ShowError("Erro critico ao executar renomeacao.", "Erro na ferramenta Rename", ex);
        }
        finally
        {
            IsEnabled = true;
        }
    }

    private bool ValidateInputs(out string errorMessage)
    {
        var maxDiffInvalid = !string.IsNullOrWhiteSpace(MaxDiffLinesInput.Text)
            && (!int.TryParse(MaxDiffLinesInput.Text, out var maxDiff) || maxDiff <= 0);

        if (!ValidationUiService.ValidateRequiredFields(
                out errorMessage,
                ValidationUiService.RequiredPath("Pasta Raiz", RootPathSelector, RootPathSelector.SelectedPath),
                ValidationUiService.RequiredControl("Texto Antigo", OldTextBox, OldTextBox.Text),
                ValidationUiService.RequiredControl("Texto Novo", NewTextBox, NewTextBox.Text)))
        {
            ValidationUiService.SetControlInvalid(MaxDiffLinesInput, maxDiffInvalid);
            return false;
        }

        ValidationUiService.SetControlInvalid(MaxDiffLinesInput, maxDiffInvalid);

        if (maxDiffInvalid)
        {
            errorMessage = "Max Diff Lines por Arquivo deve ser um número inteiro maior que zero.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }
}

