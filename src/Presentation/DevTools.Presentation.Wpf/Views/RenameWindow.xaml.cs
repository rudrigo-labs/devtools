using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using DevTools.Presentation.Wpf.Services;
using DevTools.Rename.Engine;
using DevTools.Rename.Models;
using System.Collections.Generic;
using DevTools.Core.Models;
using DevTools.Core.Configuration;

namespace DevTools.Presentation.Wpf.Views;

public partial class RenameWindow : Window
{
    private readonly JobManager _jobManager;
    private readonly SettingsService _settingsService;
    private readonly ProfileManager _profileManager;

    public RenameWindow(JobManager jobManager, SettingsService settingsService, ProfileManager profileManager)
    {        InitializeComponent();
        _jobManager = jobManager;
        _settingsService = settingsService;
        _profileManager = profileManager;

        Loaded += OnLoaded;
    }

    // Construtor para o Designer
    public RenameWindow()
    {        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {        // Tentar carregar perfil padrão
        var defaultProfile = _profileManager?.GetDefaultProfile("Rename");
        if (defaultProfile != null)
        {            if (defaultProfile.Options.TryGetValue("old-text", out var old)) OldTextBox.Text = old;
            if (defaultProfile.Options.TryGetValue("new-text", out var newText)) NewTextBox.Text = newText;
            if (defaultProfile.Options.TryGetValue("include", out var inc)) IncludeBox.Text = inc;
            if (defaultProfile.Options.TryGetValue("exclude", out var exc)) ExcludeBox.Text = exc;
        }
        else
        {            // Fallback para configurações salvas anteriormente (comportamento original)
            if (!string.IsNullOrEmpty(_settingsService?.Settings.LastRenameRootPath))
                RootPathSelector.SelectedPath = _settingsService.Settings.LastRenameRootPath;
        }
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
