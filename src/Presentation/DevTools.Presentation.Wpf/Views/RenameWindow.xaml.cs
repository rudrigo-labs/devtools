using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using DevTools.Core.Models;
using DevTools.Presentation.Wpf.Services;
using DevTools.Presentation.Wpf.Utilities;
using DevTools.Rename.Engine;
using DevTools.Rename.Models;

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

        ProfileSelector.GetOptionsFunc = GetCurrentOptions;
        ProfileSelector.ProfileLoaded += LoadProfile;

        if (!string.IsNullOrEmpty(_settingsService.Settings.LastRenameRootPath))
            RootPathSelector.SelectedPath = _settingsService.Settings.LastRenameRootPath;

        if (!string.IsNullOrEmpty(_settingsService.Settings.LastRenameInclude))
            IncludeBox.Text = _settingsService.Settings.LastRenameInclude;

        if (!string.IsNullOrEmpty(_settingsService.Settings.LastRenameExclude))
            ExcludeBox.Text = _settingsService.Settings.LastRenameExclude;

        if (_settingsService.Settings.LastRenameDryRun.HasValue)
            DryRunCheck.IsChecked = _settingsService.Settings.LastRenameDryRun.Value;
    }

    private Dictionary<string, string> GetCurrentOptions()
    {
        var options = new Dictionary<string, string>();
        options["root"] = RootPathSelector.SelectedPath;
        options["old-text"] = OldTextBox.Text;
        options["new-text"] = NewTextBox.Text;
        options["mode"] = ModeCombo.SelectedIndex == 1 ? "namespace" : "general";
        options["include"] = IncludeBox.Text;
        options["exclude"] = ExcludeBox.Text;
        options["dry-run"] = (DryRunCheck.IsChecked ?? false).ToString().ToLowerInvariant();
        options["backup"] = (BackupCheck.IsChecked ?? true).ToString().ToLowerInvariant();
        options["undo-log"] = (UndoLogCheck.IsChecked ?? true).ToString().ToLowerInvariant();
        return options;
    }

    private void LoadProfile(ToolProfile profile)
    {
        if (profile.Options.TryGetValue("root", out var root)) RootPathSelector.SelectedPath = root;
        if (profile.Options.TryGetValue("old-text", out var oldText)) OldTextBox.Text = oldText;
        if (profile.Options.TryGetValue("new-text", out var newText)) NewTextBox.Text = newText;

        if (profile.Options.TryGetValue("mode", out var mode))
            ModeCombo.SelectedIndex = mode == "namespace" || mode == "2" ? 1 : 0;

        if (profile.Options.TryGetValue("include", out var inc)) IncludeBox.Text = inc;
        if (profile.Options.TryGetValue("exclude", out var exc)) ExcludeBox.Text = exc;

        if (profile.Options.TryGetValue("dry-run", out var dry))
            DryRunCheck.IsChecked = bool.TryParse(dry, out var d) ? d : true;

        if (profile.Options.TryGetValue("backup", out var back))
            BackupCheck.IsChecked = bool.TryParse(back, out var b) ? b : true;

        if (profile.Options.TryGetValue("undo-log", out var undo))
            UndoLogCheck.IsChecked = bool.TryParse(undo, out var u) ? u : true;
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }



    private async void ProcessButton_Click(object sender, RoutedEventArgs e)
    {
        var root = RootPathSelector.SelectedPath;
        var oldText = OldTextBox.Text;
        var newText = NewTextBox.Text;

        if (string.IsNullOrWhiteSpace(root) || string.IsNullOrEmpty(oldText))
        {
            DevToolsMessage.Warning("Pasta Raiz e Texto Antigo são obrigatórios.", "Dados Incompletos");
            return;
        }

        if (!System.IO.Directory.Exists(root))
        {
            DevToolsMessage.Error("O diretório raiz especificado não existe.", "Diretório Inválido");
            return;
        }

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
            NewText: newText ?? string.Empty,
            Mode: mode,
            DryRun: dryRun,
            IncludeGlobs: string.IsNullOrWhiteSpace(include) ? null : include.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            ExcludeGlobs: string.IsNullOrWhiteSpace(exclude) ? null : exclude.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            BackupEnabled: backup,
            WriteUndoLog: undo,
            ReportPath: null,
            UndoLogPath: null,
            MaxDiffLinesPerFile: 200);

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
            DevToolsMessage.Error($"Erro crítico ao executar: {ex.Message}", "Erro");
        }
        finally
        {
            IsEnabled = true;
        }
    }
}
