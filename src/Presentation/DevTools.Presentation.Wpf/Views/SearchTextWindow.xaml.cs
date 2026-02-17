using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.Generic;
using DevTools.Presentation.Wpf.Services;
using DevTools.SearchText.Engine;
using DevTools.SearchText.Models;
using DevTools.Core.Models;

namespace DevTools.Presentation.Wpf.Views;

public partial class SearchTextWindow : Window
{
    private readonly JobManager _jobManager;
    private readonly SettingsService _settings;

    public SearchTextWindow(JobManager jobManager, SettingsService settings)
    {
        InitializeComponent();
        _jobManager = jobManager;
        _settings = settings;

        ProfileSelector.GetOptionsFunc = GetCurrentOptions;
        ProfileSelector.ProfileLoaded += LoadProfile;

        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Restore Position disabled to enforce TrayService placement
        /*
        if (_settings.Settings.SearchTextWindowTop.HasValue)
        {
            Top = _settings.Settings.SearchTextWindowTop.Value;
            Left = _settings.Settings.SearchTextWindowLeft.Value;
        }
        */

        // Restore Inputs
        if (!string.IsNullOrEmpty(_settings.Settings.LastSearchTextRootPath))
            PathSelector.SelectedPath = _settings.Settings.LastSearchTextRootPath;

        if (!string.IsNullOrEmpty(_settings.Settings.LastSearchTextInclude))
            IncludePatternInput.Text = _settings.Settings.LastSearchTextInclude;

        if (!string.IsNullOrEmpty(_settings.Settings.LastSearchTextExclude))
            ExcludePatternInput.Text = _settings.Settings.LastSearchTextExclude;
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

    private Dictionary<string, string> GetCurrentOptions()
    {
        var options = new Dictionary<string, string>();
        options["root"] = PathSelector.SelectedPath;
        options["pattern"] = SearchTextInput.Text;
        options["regex"] = (UseRegexCheck.IsChecked ?? false).ToString().ToLowerInvariant();
        options["case-sensitive"] = (CaseSensitiveCheck.IsChecked ?? false).ToString().ToLowerInvariant();
        options["include"] = IncludePatternInput.Text;
        options["exclude"] = ExcludePatternInput.Text;
        return options;
    }

    private void LoadProfile(ToolProfile profile)
    {
        if (profile.Options.TryGetValue("root", out var root)) PathSelector.SelectedPath = root;
        if (profile.Options.TryGetValue("pattern", out var pattern)) SearchTextInput.Text = pattern;
        
        if (profile.Options.TryGetValue("regex", out var regex))
             UseRegexCheck.IsChecked = bool.TryParse(regex, out var r) ? r : false;
             
        if (profile.Options.TryGetValue("case-sensitive", out var cs))
             CaseSensitiveCheck.IsChecked = bool.TryParse(cs, out var c) ? c : false;
             
        if (profile.Options.TryGetValue("include", out var inc)) IncludePatternInput.Text = inc;
        if (profile.Options.TryGetValue("exclude", out var exc)) ExcludePatternInput.Text = exc;
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // if (e.ButtonState == MouseButtonState.Pressed) DragMove();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Execute_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateInputs(out var errorMessage))
        {
            UiMessageService.ShowError(errorMessage, "Erro de Validação");
            return;
        }

        var root = PathSelector.SelectedPath;
        var text = SearchTextInput.Text;

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
                    OutputText.Text = $"Encontrados {count} resultados em {fileCount} arquivos.\n\n" + 
                                      string.Join("\n", result.Value.Files.SelectMany(f => f.Lines.Select(m => $"{f.FullPath}:{m.LineNumber} -> {m.LineText.Trim()}")));
                });
                return $"Busca concluída: {count} ocorrências.";
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    OutputText.Text = $"ERRO:\n{string.Join("\n", result.Errors.Select(e => e.Message))}";
                });
                return "Falha na busca.";
            }
        });
    }

    private string[] ParsePatterns(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return Array.Empty<string>();
        return input.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private bool ValidateInputs(out string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(PathSelector.SelectedPath))
        {
            errorMessage = "Selecione o diretório de busca.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(SearchTextInput.Text))
        {
            errorMessage = "Informe o texto a ser pesquisado.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }
}
