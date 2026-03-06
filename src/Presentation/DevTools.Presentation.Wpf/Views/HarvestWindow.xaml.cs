using System;
using System.Collections.Generic;
using System.Windows;
using DevTools.Core.Configuration;
using DevTools.Harvest.Engine;
using DevTools.Harvest.Models;
using DevTools.Presentation.Wpf.Services;

namespace DevTools.Presentation.Wpf.Views;

public partial class HarvestWindow : Window
{
    private readonly JobManager _jobManager = null!;
    private readonly SettingsService _settingsService = null!;
    private readonly ToolConfigurationManager _toolConfigurationManager = null!;

    public HarvestRequest? Result { get; private set; }

    public HarvestWindow(JobManager jobManager, SettingsService settingsService, ToolConfigurationManager toolConfigurationManager)
    {
        InitializeComponent();
        _jobManager = jobManager;
        _settingsService = settingsService;
        _toolConfigurationManager = toolConfigurationManager;

        Loaded += OnLoaded;
    }

    // Construtor para o Designer
    public HarvestWindow()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplyDefaultConfiguration();

        if (!string.IsNullOrWhiteSpace(_settingsService.Settings.LastHarvestSourcePath))
            SourcePathSelector.SelectedPath = _settingsService.Settings.LastHarvestSourcePath;
        if (!string.IsNullOrWhiteSpace(_settingsService.Settings.LastHarvestOutputPath))
            OutputPathSelector.SelectedPath = _settingsService.Settings.LastHarvestOutputPath;
        if (!string.IsNullOrWhiteSpace(_settingsService.Settings.LastHarvestConfigPath))
            ConfigPathSelector.SelectedPath = _settingsService.Settings.LastHarvestConfigPath;
        if (_settingsService.Settings.LastHarvestMinScore.HasValue)
            MinScoreBox.Text = _settingsService.Settings.LastHarvestMinScore.Value.ToString();
        if (_settingsService.Settings.LastHarvestCopyFiles.HasValue)
            CopyFilesCheck.IsChecked = _settingsService.Settings.LastHarvestCopyFiles.Value;
    }

    private void ApplyDefaultConfiguration()
    {
        var configuration = _toolConfigurationManager.GetDefaultConfiguration("Harvest");
        if (configuration == null)
            return;

        if (configuration.Options.TryGetValue("source-path", out var sourcePath) && !string.IsNullOrWhiteSpace(sourcePath))
            SourcePathSelector.SelectedPath = sourcePath;

        if (configuration.Options.TryGetValue("output-path", out var outputPath) && !string.IsNullOrWhiteSpace(outputPath))
            OutputPathSelector.SelectedPath = outputPath;

        if (configuration.Options.TryGetValue("config-path", out var configPath) && !string.IsNullOrWhiteSpace(configPath))
            ConfigPathSelector.SelectedPath = configPath;

        if (configuration.Options.TryGetValue("min-score", out var minScore) && !string.IsNullOrWhiteSpace(minScore))
            MinScoreBox.Text = minScore;

        if (configuration.Options.TryGetValue("copy-files", out var copyFiles) && bool.TryParse(copyFiles, out var parsedCopyFiles))
            CopyFilesCheck.IsChecked = parsedCopyFiles;
    }

    private bool ValidateInputs(out string errorMessage)
    {
        var sourceMissing = string.IsNullOrWhiteSpace(SourcePathSelector.SelectedPath);
        var outputMissing = string.IsNullOrWhiteSpace(OutputPathSelector.SelectedPath);
        var minScoreMissing = string.IsNullOrWhiteSpace(MinScoreBox.Text);

        ValidationUiService.SetPathSelectorInvalid(SourcePathSelector, sourceMissing);
        ValidationUiService.SetPathSelectorInvalid(OutputPathSelector, outputMissing);
        ValidationUiService.SetControlInvalid(MinScoreBox, minScoreMissing);

        var missing = new List<string>();
        if (sourceMissing) missing.Add("Diretorio de Origem");
        if (outputMissing) missing.Add("Diretorio de Destino");
        if (minScoreMissing) missing.Add("Score Minimo");

        if (missing.Count > 0)
        {
            errorMessage = "Os campos abaixo nao podem ficar em branco:\n- " + string.Join("\n- ", missing);
            return false;
        }

        if (!int.TryParse(MinScoreBox.Text, out _))
        {
            ValidationUiService.SetControlInvalid(MinScoreBox, true);
            errorMessage = "Score minimo deve ser um numero inteiro valido.";
            return false;
        }

        ValidationUiService.SetControlInvalid(MinScoreBox, false);
        errorMessage = string.Empty;
        return true;
    }

    private async void Run_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateInputs(out var validationError))
        {
            ValidationUiService.ShowInline(MainFrame, validationError);
            return;
        }

        ValidationUiService.ClearInline(MainFrame);
        var rootPath = SourcePathSelector.SelectedPath!;
        var outputPath = OutputPathSelector.SelectedPath!;

        // Salvar configuracoes apenas se opt-in.
        if (RememberSettingsCheck.IsChecked == true)
        {
            _settingsService.Settings.LastHarvestSourcePath = SourcePathSelector.SelectedPath;
            _settingsService.Settings.LastHarvestOutputPath = OutputPathSelector.SelectedPath;
            _settingsService.Settings.LastHarvestConfigPath = ConfigPathSelector.SelectedPath;

            if (int.TryParse(MinScoreBox.Text, out var minScore))
                _settingsService.Settings.LastHarvestMinScore = minScore;

            _settingsService.Settings.LastHarvestCopyFiles = CopyFilesCheck.IsChecked;
            _settingsService.Save();
        }

        Result = new HarvestRequest(
            RootPath: rootPath,
            OutputPath: outputPath,
            ConfigPath: ConfigPathSelector.SelectedPath,
            MinScore: int.TryParse(MinScoreBox.Text, out var ms) ? ms : 0,
            CopyFiles: CopyFilesCheck.IsChecked ?? true
        );

        var engine = new HarvestEngine();

        IsEnabled = false;
        RunSummary.Clear();

        try
        {
            var result = await System.Threading.Tasks.Task.Run(() => engine.ExecuteAsync(Result));
            RunSummary.BindResult(result);
        }
        catch (Exception ex)
        {
            UiMessageService.ShowError("Erro critico ao executar coleta.", "Erro na ferramenta Harvest", ex);
        }
        finally
        {
            IsEnabled = true;
        }
    }

    private void Header_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // DragMove();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

