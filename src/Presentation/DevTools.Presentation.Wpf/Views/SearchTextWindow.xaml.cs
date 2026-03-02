using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using DevTools.Core.Models;
using DevTools.Presentation.Wpf.Services;
using DevTools.Presentation.Wpf.Utilities;
using DevTools.SearchText.Engine;
using DevTools.SearchText.Models;

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

        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
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

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Execute_Click(object sender, RoutedEventArgs e)
    {
        var root = PathSelector.SelectedPath;
        var text = SearchTextInput.Text;

        if (string.IsNullOrWhiteSpace(root))
        {
            DevToolsMessage.Warning("Selecione o diretório de busca.", "Atenção");
            return;
        }

        if (!System.IO.Directory.Exists(root))
        {
            DevToolsMessage.Error("O diretório de busca especificado não existe.", "Diretório Inválido");
            return;
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            DevToolsMessage.Warning("Informe o texto a ser pesquisado.", "Atenção");
            return;
        }

        var request = new SearchTextRequest(
            RootPath: root,
            Pattern: text,
            UseRegex: UseRegexCheck.IsChecked == true,
            CaseSensitive: CaseSensitiveCheck.IsChecked == true,
            IncludeGlobs: ParsePatterns(IncludePatternInput.Text),
            ExcludeGlobs: ParsePatterns(ExcludePatternInput.Text));

        OutputText.Text = "Buscando...";

        _jobManager.StartJob("Busca de Texto", async (progress, ct) =>
        {
            try
            {
                var engine = new SearchTextEngine();
                var result = await engine.ExecuteAsync(request, progress, ct);

                if (result.IsSuccess && result.Value != null)
                {
                    var count = result.Value.TotalOccurrences;
                    var fileCount = result.Value.TotalFilesWithMatches;
                    var matches = result.Value.Files.SelectMany(f => f.Lines.Select(m => $"{f.FullPath}:{m.LineNumber} -> {m.LineText.Trim()}"));

                    Dispatcher.Invoke(() =>
                    {
                        OutputText.Text = $"Encontrados {count} resultados em {fileCount} arquivos.\n\n" +
                                          string.Join("\n", matches);
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
            }
            catch (Exception ex)
            {
                AppLogger.Error("Erro crítico na busca", ex);
                Dispatcher.Invoke(() => OutputText.Text = $"ERRO CRÍTICO: {ex.Message}");
                return $"Erro crítico: {ex.Message}";
            }
        });
    }

    private string[] ParsePatterns(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return Array.Empty<string>();
        return input.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}

