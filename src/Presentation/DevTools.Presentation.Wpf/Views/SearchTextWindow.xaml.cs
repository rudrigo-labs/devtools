using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DevTools.Core.Configuration;
using DevTools.Presentation.Wpf.Services;
using DevTools.SearchText.Engine;
using DevTools.SearchText.Models;

namespace DevTools.Presentation.Wpf.Views;

public partial class SearchTextWindow : Window
{
    private readonly JobManager _jobManager = null!;
    private readonly SettingsService _settings = null!;

    public SearchTextWindow(JobManager jobManager, SettingsService settings, ToolConfigurationManager toolConfigurationManager)
    {
        InitializeComponent();
        _jobManager = jobManager;
        _settings = settings;

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
        if (!string.IsNullOrWhiteSpace(_settings.Settings.LastSearchTextRootPath))
            PathSelector.SelectedPath = _settings.Settings.LastSearchTextRootPath;
        if (!string.IsNullOrWhiteSpace(_settings.Settings.LastSearchTextInclude))
            IncludePatternInput.Text = _settings.Settings.LastSearchTextInclude;
        if (!string.IsNullOrWhiteSpace(_settings.Settings.LastSearchTextExclude))
            ExcludePatternInput.Text = _settings.Settings.LastSearchTextExclude;
        else
            ExcludePatternInput.Text = string.Join(", ", SearchTextDefaults.DefaultExcludeGlobs);

        WholeWordCheck.IsChecked = _settings.Settings.LastSearchTextWholeWord ?? false;
        SkipBinaryFilesCheck.IsChecked = _settings.Settings.LastSearchTextSkipBinaryFiles ?? true;
        ReturnLinesCheck.IsChecked = _settings.Settings.LastSearchTextReturnLines ?? true;
        MaxFileSizeKbInput.Text = _settings.Settings.LastSearchTextMaxFileSizeKb?.ToString() ?? string.Empty;
        MaxMatchesPerFileInput.Text = (_settings.Settings.LastSearchTextMaxMatchesPerFile ?? 0).ToString();
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _settings.Settings.SearchTextWindowTop = Top;
        _settings.Settings.SearchTextWindowLeft = Left;
        _settings.Settings.LastSearchTextRootPath = PathSelector.SelectedPath;
        _settings.Settings.LastSearchTextInclude = IncludePatternInput.Text;
        _settings.Settings.LastSearchTextExclude = ExcludePatternInput.Text;
        _settings.Settings.LastSearchTextWholeWord = WholeWordCheck.IsChecked == true;
        _settings.Settings.LastSearchTextSkipBinaryFiles = SkipBinaryFilesCheck.IsChecked == true;
        _settings.Settings.LastSearchTextReturnLines = ReturnLinesCheck.IsChecked == true;
        _settings.Settings.LastSearchTextMaxFileSizeKb = ParseOptionalInt(MaxFileSizeKbInput.Text);
        _settings.Settings.LastSearchTextMaxMatchesPerFile = ParseOptionalInt(MaxMatchesPerFileInput.Text) ?? 0;
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
        var maxFileSizeKb = ParseOptionalInt(MaxFileSizeKbInput.Text);
        var maxMatchesPerFile = ParseOptionalInt(MaxMatchesPerFileInput.Text) ?? 0;

        var request = new SearchTextRequest(
            RootPath: root,
            Pattern: text,
            UseRegex: UseRegexCheck.IsChecked == true,
            CaseSensitive: CaseSensitiveCheck.IsChecked == true,
            WholeWord: WholeWordCheck.IsChecked == true,
            IncludeGlobs: ParsePatterns(IncludePatternInput.Text),
            ExcludeGlobs: ParsePatterns(ExcludePatternInput.Text),
            MaxFileSizeKb: maxFileSizeKb,
            SkipBinaryFiles: SkipBinaryFilesCheck.IsChecked != false,
            MaxMatchesPerFile: maxMatchesPerFile,
            ReturnLines: ReturnLinesCheck.IsChecked != false
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

        if (!string.IsNullOrWhiteSpace(MaxFileSizeKbInput.Text) && ParseOptionalInt(MaxFileSizeKbInput.Text) is null)
        {
            errorMessage = "Tamanho Máximo por Arquivo (KB) deve ser um número inteiro válido.";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(MaxMatchesPerFileInput.Text) && ParseOptionalInt(MaxMatchesPerFileInput.Text) is null)
        {
            errorMessage = "Máximo de Ocorrências por Arquivo deve ser um número inteiro válido.";
            return false;
        }

        var maxMatches = ParseOptionalInt(MaxMatchesPerFileInput.Text) ?? 0;
        if (maxMatches < 0)
        {
            errorMessage = "Máximo de Ocorrências por Arquivo deve ser maior ou igual a zero.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    private static int? ParseOptionalInt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return int.TryParse(value, out var parsed) ? parsed : null;
    }
}

