using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using DevTools.Core.Configuration;
using DevTools.Core.Models;
using DevTools.Presentation.Wpf.Services;
using DevTools.Snapshot.Engine;
using DevTools.Snapshot.Models;

namespace DevTools.Presentation.Wpf.Views;

public partial class SnapshotWindow : Window
{
    private readonly JobManager _jobManager = null!;
    private readonly SettingsService _settingsService = null!;
    private readonly ToolConfigurationManager _toolConfigurationManager = null!;
    private ToolConfiguration? _currentConfiguration;

    public SnapshotWindow(JobManager jobManager, SettingsService settingsService, ToolConfigurationManager toolConfigurationManager)
    {
        InitializeComponent();
        _jobManager = jobManager;
        _settingsService = settingsService;
        _toolConfigurationManager = toolConfigurationManager;

        Loaded += OnLoaded;
    }

    // Construtor para o Designer
    public SnapshotWindow()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_settingsService.Settings.LastSnapshotRootPath))
            RootPathSelector.SelectedPath = _settingsService.Settings.LastSnapshotRootPath;
        if (!string.IsNullOrWhiteSpace(_settingsService.Settings.LastSnapshotOutputBasePath))
            OutputBasePathSelector.SelectedPath = _settingsService.Settings.LastSnapshotOutputBasePath;
        if (!string.IsNullOrWhiteSpace(_settingsService.Settings.LastSnapshotIgnoredDirectories))
            IgnoredDirectoriesInput.Text = _settingsService.Settings.LastSnapshotIgnoredDirectories;
        if (_settingsService.Settings.LastSnapshotMaxFileSizeKb.HasValue)
            MaxFileSizeKbInput.Text = _settingsService.Settings.LastSnapshotMaxFileSizeKb.Value.ToString();
        if (_settingsService.Settings.LastSnapshotGenerateText.HasValue)
            TextCheck.IsChecked = _settingsService.Settings.LastSnapshotGenerateText.Value;
        if (_settingsService.Settings.LastSnapshotGenerateHtml.HasValue)
            HtmlCheck.IsChecked = _settingsService.Settings.LastSnapshotGenerateHtml.Value;
        if (_settingsService.Settings.LastSnapshotGenerateJsonNested.HasValue)
            JsonNestedCheck.IsChecked = _settingsService.Settings.LastSnapshotGenerateJsonNested.Value;
        if (_settingsService.Settings.LastSnapshotGenerateJsonRecursive.HasValue)
            JsonRecursiveCheck.IsChecked = _settingsService.Settings.LastSnapshotGenerateJsonRecursive.Value;

        _currentConfiguration = _toolConfigurationManager?.GetDefaultConfiguration("Snapshot");
        if (_currentConfiguration != null)
        {
            if (_currentConfiguration.Options.TryGetValue("project-path", out var proj)) RootPathSelector.SelectedPath = proj;
            if (_currentConfiguration.Options.TryGetValue("output-base-path", out var outputBasePath)) OutputBasePathSelector.SelectedPath = outputBasePath;
            if (_currentConfiguration.Options.TryGetValue("ignored-directories", out var ignoredDirectories)) IgnoredDirectoriesInput.Text = ignoredDirectories;
            if (_currentConfiguration.Options.TryGetValue("max-file-size-kb", out var maxFileSize)) MaxFileSizeKbInput.Text = maxFileSize;
            if (_currentConfiguration.Options.TryGetValue("generate-text", out var genText) && bool.TryParse(genText, out var textEnabled)) TextCheck.IsChecked = textEnabled;
            if (_currentConfiguration.Options.TryGetValue("generate-html", out var genHtml) && bool.TryParse(genHtml, out var htmlEnabled)) HtmlCheck.IsChecked = htmlEnabled;
            if (_currentConfiguration.Options.TryGetValue("generate-json-nested", out var genJsonNested) && bool.TryParse(genJsonNested, out var jsonNestedEnabled)) JsonNestedCheck.IsChecked = jsonNestedEnabled;
            if (_currentConfiguration.Options.TryGetValue("generate-json-recursive", out var genJsonRecursive) && bool.TryParse(genJsonRecursive, out var jsonRecursiveEnabled)) JsonRecursiveCheck.IsChecked = jsonRecursiveEnabled;
        }
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

        string root = RootPathSelector.SelectedPath ?? string.Empty;
        var outputBasePath = string.IsNullOrWhiteSpace(OutputBasePathSelector.SelectedPath) ? null : OutputBasePathSelector.SelectedPath;
        var ignoredDirectories = ParsePatterns(IgnoredDirectoriesInput.Text);
        var maxFileSizeKb = ParseOptionalInt(MaxFileSizeKbInput.Text);
        var genText = TextCheck.IsChecked ?? true;
        var genHtml = HtmlCheck.IsChecked ?? false;
        var genJsonNested = JsonNestedCheck.IsChecked ?? false;
        var genJsonRecursive = JsonRecursiveCheck.IsChecked ?? false;

        Close();

        _settingsService.Settings.LastSnapshotRootPath = root;
        _settingsService.Settings.LastSnapshotOutputBasePath = outputBasePath;
        _settingsService.Settings.LastSnapshotIgnoredDirectories = IgnoredDirectoriesInput.Text;
        _settingsService.Settings.LastSnapshotMaxFileSizeKb = maxFileSizeKb;
        _settingsService.Settings.LastSnapshotGenerateText = genText;
        _settingsService.Settings.LastSnapshotGenerateHtml = genHtml;
        _settingsService.Settings.LastSnapshotGenerateJsonNested = genJsonNested;
        _settingsService.Settings.LastSnapshotGenerateJsonRecursive = genJsonRecursive;
        _settingsService.Save();

        if (_currentConfiguration != null)
        {
            _currentConfiguration.Options["project-path"] = root;
            _currentConfiguration.Options["output-base-path"] = outputBasePath ?? string.Empty;
            _currentConfiguration.Options["ignored-directories"] = IgnoredDirectoriesInput.Text ?? string.Empty;
            _currentConfiguration.Options["max-file-size-kb"] = maxFileSizeKb?.ToString() ?? string.Empty;
            _currentConfiguration.Options["generate-text"] = genText.ToString();
            _currentConfiguration.Options["generate-html"] = genHtml.ToString();
            _currentConfiguration.Options["generate-json-nested"] = genJsonNested.ToString();
            _currentConfiguration.Options["generate-json-recursive"] = genJsonRecursive.ToString();
            _toolConfigurationManager.SaveConfiguration("Snapshot", _currentConfiguration);
        }

        _jobManager.StartJob("Snapshot", async (reporter, ct) =>
        {
            var engine = new SnapshotEngine();
            var request = new SnapshotRequest(
                RootPath: root,
                OutputBasePath: outputBasePath,
                GenerateText: genText,
                GenerateHtmlPreview: genHtml,
                GenerateJsonNested: genJsonNested,
                GenerateJsonRecursive: genJsonRecursive,
                IgnoredDirectories: ignoredDirectories,
                MaxFileSizeKb: maxFileSizeKb
            );

            var result = await engine.ExecuteAsync(request, reporter, ct);

            return result.IsSuccess
                ? "Snapshot gerado com sucesso na pasta 'Snapshot'!"
                : $"Falha ao gerar Snapshot: {string.Join(", ", result.Errors.Select(x => x.Message))}";
        });
    }

    private bool ValidateInputs(out string errorMessage)
    {
        var rootMissing = string.IsNullOrWhiteSpace(RootPathSelector.SelectedPath);
        var maxFileSizeInvalid = !string.IsNullOrWhiteSpace(MaxFileSizeKbInput.Text) && ParseOptionalInt(MaxFileSizeKbInput.Text) is null;

        ValidationUiService.SetPathSelectorInvalid(RootPathSelector, rootMissing);
        ValidationUiService.SetControlInvalid(MaxFileSizeKbInput, maxFileSizeInvalid);

        if (rootMissing)
        {
            errorMessage = "Os campos abaixo nao podem ficar em branco:\n- Pasta do Projeto";
            return false;
        }

        if (maxFileSizeInvalid)
        {
            errorMessage = "Tamanho Máximo por Arquivo (KB) deve ser um número inteiro válido.";
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

    private static int? ParseOptionalInt(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        return int.TryParse(input, out var parsed) ? parsed : null;
    }
}


