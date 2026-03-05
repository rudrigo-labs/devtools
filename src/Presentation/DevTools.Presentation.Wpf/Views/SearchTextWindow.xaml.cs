using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DevTools.Core.Configuration;
using DevTools.Core.Models;
using DevTools.Presentation.Wpf.Services;
using DevTools.SearchText.Engine;
using DevTools.SearchText.Models;

namespace DevTools.Presentation.Wpf.Views;

public partial class SearchTextWindow : Window
{
    private readonly JobManager _jobManager = null!;
    private readonly SettingsService _settings = null!;
    private readonly ProfileManager _profileManager = null!;
    private ToolProfile? _currentProfile;

    public SearchTextWindow(JobManager jobManager, SettingsService settings, ProfileManager profileManager)
    {
        InitializeComponent();
        _jobManager = jobManager;
        _settings = settings;
        _profileManager = profileManager;

        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    // Construtor para o Designer
    public SearchTextWindow()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _currentProfile = _profileManager?.GetDefaultProfile("SearchText");
        if (_currentProfile != null)
        {
            if (_currentProfile.Options.TryGetValue("root-path", out var root)) PathSelector.SelectedPath = root;
            if (_currentProfile.Options.TryGetValue("search-pattern", out var pattern)) SearchTextInput.Text = pattern;
            if (_currentProfile.Options.TryGetValue("include", out var inc)) IncludePatternInput.Text = inc;
            if (_currentProfile.Options.TryGetValue("exclude", out var exc)) ExcludePatternInput.Text = exc;
            return;
        }
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _settings.Settings.SearchTextWindowTop = Top;
        _settings.Settings.SearchTextWindowLeft = Left;
        _settings.Settings.LastSearchTextRootPath = PathSelector.SelectedPath;
        _settings.Settings.LastSearchTextInclude = IncludePatternInput.Text;
        _settings.Settings.LastSearchTextExclude = ExcludePatternInput.Text;
        _settings.Save();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Execute_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateInputs(out var errorMessage))
        {
            ValidationUiService.ShowInline(MainFrame, errorMessage);
            return;
        }

        ValidationUiService.ClearInline(MainFrame);

        string root = PathSelector.SelectedPath ?? string.Empty;
        string text = SearchTextInput.Text ?? string.Empty;

        if (_currentProfile != null)
        {
            _currentProfile.Options["root-path"] = root;
            _currentProfile.Options["search-pattern"] = text;
            _currentProfile.Options["include"] = IncludePatternInput.Text ?? string.Empty;
            _currentProfile.Options["exclude"] = ExcludePatternInput.Text ?? string.Empty;
            _profileManager.SaveProfile("SearchText", _currentProfile);
        }

        var request = new SearchTextRequest(
            RootPath: root,
            Pattern: text,
            UseRegex: UseRegexCheck.IsChecked == true,
            CaseSensitive: CaseSensitiveCheck.IsChecked == true,
            IncludeGlobs: ParsePatterns(IncludePatternInput.Text),
            ExcludeGlobs: ParsePatterns(ExcludePatternInput.Text)
        );

        OutputText.Text = "Buscando...";

        _jobManager.StartJob("Busca de Texto", async (progress, ct) =>
        {
            var engine = new SearchTextEngine();
            var result = await engine.ExecuteAsync(request, progress, ct);

            if (result.IsSuccess && result.Value != null)
            {
                var count = result.Value.TotalOccurrences;
                var fileCount = result.Value.TotalFilesWithMatches;

                Dispatcher.Invoke(() =>
                {
                    OutputText.Text = $"Encontrados {count} resultados em {fileCount} arquivos.\n\n"
                        + string.Join("\n", result.Value.Files.SelectMany(f => f.Lines.Select(m => $"{f.FullPath}:{m.LineNumber} -> {m.LineText.Trim()}")));
                });
                return $"Busca concluida: {count} ocorrencias.";
            }

            Dispatcher.Invoke(() =>
            {
                OutputText.Text = $"ERRO:\n{string.Join("\n", result.Errors.Select(e => e.Message))}";
            });
            return "Falha na busca.";
        });
    }

    private static string[] ParsePatterns(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return Array.Empty<string>();
        return input.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private bool ValidateInputs(out string errorMessage)
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(PathSelector.SelectedPath))
            missing.Add("Diretorio de Busca");
        if (string.IsNullOrWhiteSpace(SearchTextInput.Text))
            missing.Add("Texto de Pesquisa");

        if (missing.Count > 0)
        {
            errorMessage = "Os campos abaixo nao podem ficar em branco:\n- " + string.Join("\n- ", missing);
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }
}
