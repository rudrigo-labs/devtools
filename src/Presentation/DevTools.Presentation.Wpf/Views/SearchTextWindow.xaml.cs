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
    private readonly ToolConfigurationManager _toolConfigurationManager = null!;

    public SearchTextWindow(JobManager jobManager, SettingsService settings, ToolConfigurationManager toolConfigurationManager)
    {
        InitializeComponent();
        _jobManager = jobManager;
        _settings = settings;
        _toolConfigurationManager = toolConfigurationManager;

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
        ApplyDefaultConfiguration();

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

    private void ApplyDefaultConfiguration()
    {
        var configuration = _toolConfigurationManager.GetDefaultConfiguration("SearchText");
        if (configuration == null)
            return;

        if (configuration.Options.TryGetValue("root-path", out var rootPath) && !string.IsNullOrWhiteSpace(rootPath))
            PathSelector.SelectedPath = rootPath;

        if (configuration.Options.TryGetValue("search-pattern", out var pattern))
            SearchTextInput.Text = pattern ?? string.Empty;

        if (configuration.Options.TryGetValue("include", out var include))
            IncludePatternInput.Text = include ?? string.Empty;

        if (configuration.Options.TryGetValue("exclude", out var exclude))
            ExcludePatternInput.Text = exclude ?? string.Empty;

        if (configuration.Options.TryGetValue("max-file-size-kb", out var maxFileSize))
            MaxFileSizeKbInput.Text = maxFileSize ?? string.Empty;

        if (configuration.Options.TryGetValue("max-matches-per-file", out var maxMatches) && !string.IsNullOrWhiteSpace(maxMatches))
            MaxMatchesPerFileInput.Text = maxMatches;

        if (configuration.Options.TryGetValue("use-regex", out var useRegex) && bool.TryParse(useRegex, out var parsedUseRegex))
            UseRegexCheck.IsChecked = parsedUseRegex;

        if (configuration.Options.TryGetValue("case-sensitive", out var caseSensitive) && bool.TryParse(caseSensitive, out var parsedCaseSensitive))
            CaseSensitiveCheck.IsChecked = parsedCaseSensitive;

        if (configuration.Options.TryGetValue("whole-word", out var wholeWord) && bool.TryParse(wholeWord, out var parsedWholeWord))
            WholeWordCheck.IsChecked = parsedWholeWord;

        if (configuration.Options.TryGetValue("skip-binary-files", out var skipBinary) && bool.TryParse(skipBinary, out var parsedSkipBinary))
            SkipBinaryFilesCheck.IsChecked = parsedSkipBinary;

        if (configuration.Options.TryGetValue("return-lines", out var returnLines) && bool.TryParse(returnLines, out var parsedReturnLines))
            ReturnLinesCheck.IsChecked = parsedReturnLines;
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
        var rootMissing = string.IsNullOrWhiteSpace(PathSelector.SelectedPath);
        var patternMissing = string.IsNullOrWhiteSpace(SearchTextInput.Text);
        var maxFileSizeInvalid = !string.IsNullOrWhiteSpace(MaxFileSizeKbInput.Text) && ParseOptionalInt(MaxFileSizeKbInput.Text) is null;
        var maxMatchesInvalid = !string.IsNullOrWhiteSpace(MaxMatchesPerFileInput.Text) && ParseOptionalInt(MaxMatchesPerFileInput.Text) is null;
        var maxMatchesNegative = !maxMatchesInvalid && (ParseOptionalInt(MaxMatchesPerFileInput.Text) ?? 0) < 0;

        ValidationUiService.SetPathSelectorInvalid(PathSelector, rootMissing);
        ValidationUiService.SetControlInvalid(SearchTextInput, patternMissing);
        ValidationUiService.SetControlInvalid(MaxFileSizeKbInput, maxFileSizeInvalid);
        ValidationUiService.SetControlInvalid(MaxMatchesPerFileInput, maxMatchesInvalid || maxMatchesNegative);

        var missing = new List<string>();
        if (rootMissing)
            missing.Add("Diretorio de Busca");
        if (patternMissing)
            missing.Add("Texto de Pesquisa");

        if (missing.Count > 0)
        {
            errorMessage = "Os campos abaixo nao podem ficar em branco:\n- " + string.Join("\n- ", missing);
            return false;
        }

        if (maxFileSizeInvalid)
        {
            errorMessage = "Tamanho Máximo por Arquivo (KB) deve ser um número inteiro válido.";
            return false;
        }

        if (maxMatchesInvalid)
        {
            errorMessage = "Máximo de Ocorrências por Arquivo deve ser um número inteiro válido.";
            return false;
        }

        if (maxMatchesNegative)
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

