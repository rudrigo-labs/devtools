using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.Generic;
using DevTools.Presentation.Wpf.Services;
using DevTools.SearchText.Engine;
using DevTools.SearchText.Models;
using DevTools.Core.Models;

using DevTools.Core.Configuration;

namespace DevTools.Presentation.Wpf.Views;

public partial class SearchTextWindow : Window
{
    private readonly JobManager _jobManager;
    private readonly SettingsService _settings;
    private readonly ProfileManager _profileManager;
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
        // Tentar carregar perfil padrão
        _currentProfile = _profileManager?.GetDefaultProfile("SearchText");
        if (_currentProfile != null)
        {
            if (_currentProfile.Options.TryGetValue("root-path", out var root)) PathSelector.SelectedPath = root;
            if (_currentProfile.Options.TryGetValue("search-pattern", out var pattern)) SearchTextInput.Text = pattern;
            if (_currentProfile.Options.TryGetValue("include", out var inc)) IncludePatternInput.Text = inc;
            if (_currentProfile.Options.TryGetValue("exclude", out var exc)) ExcludePatternInput.Text = exc;
        }
        else
        {
            // Fallback para configurações salvas anteriormente (comportamento original)
            if (!string.IsNullOrEmpty(_settings?.Settings.LastSearchTextRootPath))
                PathSelector.SelectedPath = _settings.Settings.LastSearchTextRootPath;

            if (!string.IsNullOrEmpty(_settings?.Settings.LastSearchTextInclude))
                IncludePatternInput.Text = _settings.Settings.LastSearchTextInclude;

            if (!string.IsNullOrEmpty(_settings?.Settings.LastSearchTextExclude))
                ExcludePatternInput.Text = _settings.Settings.LastSearchTextExclude;
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
            UiMessageService.ShowError(errorMessage, "Erro de Validação");
            return;
        }

        var root = PathSelector.SelectedPath;
        var text = SearchTextInput.Text;

        // Sincronizar com o perfil padrão se estiver em uso
        if (_currentProfile != null)
        {
            _currentProfile.Options["root-path"] = root ?? "";
            _currentProfile.Options["search-pattern"] = text ?? "";
            _currentProfile.Options["include"] = IncludePatternInput.Text ?? "";
            _currentProfile.Options["exclude"] = ExcludePatternInput.Text ?? "";
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
