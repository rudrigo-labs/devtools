using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using DevTools.Core.Configuration;
using DevTools.Core.Models;
using DevTools.Presentation.Wpf.Services;
using DevTools.Utf8Convert.Engine;
using DevTools.Utf8Convert.Models;

namespace DevTools.Presentation.Wpf.Views;

public partial class Utf8ConvertWindow : Window
{
    private readonly JobManager _jobManager;
    private readonly SettingsService _settingsService;
    private readonly ToolConfigurationManager _toolConfigurationManager;

    public Utf8ConvertWindow(JobManager jobManager, SettingsService settingsService, ToolConfigurationManager toolConfigurationManager)
    {
        InitializeComponent();
        _jobManager = jobManager;
        _settingsService = settingsService;
        _toolConfigurationManager = toolConfigurationManager;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplyDefaultConfiguration();

        if (!string.IsNullOrWhiteSpace(_settingsService.Settings.LastUtf8RootPath))
            RootPathSelector.SelectedPath = _settingsService.Settings.LastUtf8RootPath;

        DryRunCheck.IsChecked = _settingsService.Settings.LastUtf8DryRun ?? false;
        IncludeGlobsInput.Text = _settingsService.Settings.LastUtf8IncludeGlobs ?? string.Empty;
        ExcludeGlobsInput.Text = _settingsService.Settings.LastUtf8ExcludeGlobs ?? string.Empty;
    }

    private void ApplyDefaultConfiguration()
    {
        var configuration = _toolConfigurationManager.GetDefaultConfiguration("Utf8Convert");
        if (configuration == null)
            return;

        if (configuration.Options.TryGetValue("root-path", out var rootPath) && !string.IsNullOrWhiteSpace(rootPath))
            RootPathSelector.SelectedPath = rootPath;

        if (configuration.Options.TryGetValue("include", out var include))
            IncludeGlobsInput.Text = include ?? string.Empty;

        if (configuration.Options.TryGetValue("exclude", out var exclude))
            ExcludeGlobsInput.Text = exclude ?? string.Empty;

        if (configuration.Options.TryGetValue("recursive", out var recursive) && bool.TryParse(recursive, out var parsedRecursive))
            RecursiveCheck.IsChecked = parsedRecursive;

        if (configuration.Options.TryGetValue("dry-run", out var dryRun) && bool.TryParse(dryRun, out var parsedDryRun))
            DryRunCheck.IsChecked = parsedDryRun;

        if (configuration.Options.TryGetValue("create-backup", out var createBackup) && bool.TryParse(createBackup, out var parsedBackup))
            BackupCheck.IsChecked = parsedBackup;

        if (configuration.Options.TryGetValue("output-bom", out var outputBom) && bool.TryParse(outputBom, out var parsedOutputBom))
            BomCheck.IsChecked = parsedOutputBom;
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // if (e.ButtonState == MouseButtonState.Pressed) DragMove();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void BrowseRoot_Click(object sender, RoutedEventArgs e) { }

    private void ProcessButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateInputs(out var errorMessage))
        {
            ValidationUiService.ShowInline(MainFrame, errorMessage);
            return;
        }

        ValidationUiService.ClearInline(MainFrame);

        var root = RootPathSelector.SelectedPath;
        var recursive = RecursiveCheck.IsChecked ?? true;
        var dryRun = DryRunCheck.IsChecked ?? false;
        var backup = BackupCheck.IsChecked ?? true;
        var bom = BomCheck.IsChecked ?? true;
        var include = ParsePatterns(IncludeGlobsInput.Text);
        var exclude = ParsePatterns(ExcludeGlobsInput.Text);

        _settingsService.Settings.LastUtf8RootPath = root;
        _settingsService.Settings.LastUtf8DryRun = dryRun;
        _settingsService.Settings.LastUtf8IncludeGlobs = IncludeGlobsInput.Text;
        _settingsService.Settings.LastUtf8ExcludeGlobs = ExcludeGlobsInput.Text;
        _settingsService.Save();

        Close();

        _jobManager.StartJob("Utf8Convert", async (reporter, ct) =>
        {
            var engine = new Utf8ConvertEngine();
            var request = new Utf8ConvertRequest(
                RootPath: root,
                Recursive: recursive,
                DryRun: dryRun,
                CreateBackup: backup,
                OutputBom: bom,
                IncludeGlobs: include,
                ExcludeGlobs: exclude
            );

            var result = await engine.ExecuteAsync(request, reporter, ct);

            return result.IsSuccess
                ? $"Conversao concluida! {result.Value?.Summary.Converted ?? 0} arquivos convertidos."
                : $"Falha na conversao: {string.Join(", ", result.Errors.Select(x => x.Message))}";
        });
    }

    private bool ValidateInputs(out string errorMessage)
    {
        var rootMissing = string.IsNullOrWhiteSpace(RootPathSelector.SelectedPath);
        ValidationUiService.SetPathSelectorInvalid(RootPathSelector, rootMissing);

        if (rootMissing)
        {
            errorMessage = "Os campos abaixo nao podem ficar em branco:\n- Pasta Raiz";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    private static string[] ParsePatterns(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Array.Empty<string>();

        return input.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
