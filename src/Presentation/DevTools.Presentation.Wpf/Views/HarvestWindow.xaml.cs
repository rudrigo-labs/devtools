using System;
using System.Linq;
using System.Windows;
using System.Collections.Generic;
using DevTools.Harvest.Engine;
using DevTools.Harvest.Models;
using DevTools.Presentation.Wpf.Services;
using DevTools.Presentation.Wpf.Utilities;
using DevTools.Core.Models;

namespace DevTools.Presentation.Wpf.Views;

public partial class HarvestWindow : Window
{
    private readonly JobManager _jobManager;
    private readonly SettingsService _settingsService;
    public HarvestRequest? Result { get; private set; }

    public HarvestWindow(JobManager jobManager, SettingsService settingsService)
    {
        InitializeComponent();
        _jobManager = jobManager;
        _settingsService = settingsService;

        if (!string.IsNullOrEmpty(_settingsService.Settings.LastHarvestSourcePath))
            SourcePathSelector.SelectedPath = _settingsService.Settings.LastHarvestSourcePath;
            
        if (!string.IsNullOrEmpty(_settingsService.Settings.LastHarvestOutputPath))
            OutputPathSelector.SelectedPath = _settingsService.Settings.LastHarvestOutputPath;

        if (!string.IsNullOrEmpty(_settingsService.Settings.LastHarvestConfigPath))
            ConfigPathSelector.SelectedPath = _settingsService.Settings.LastHarvestConfigPath;

        if (_settingsService.Settings.LastHarvestMinScore.HasValue)
            MinScoreBox.Text = _settingsService.Settings.LastHarvestMinScore.Value.ToString();

        if (_settingsService.Settings.LastHarvestCopyFiles.HasValue)
            CopyFilesCheck.IsChecked = _settingsService.Settings.LastHarvestCopyFiles.Value;
    }

    private async void Run_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(SourcePathSelector.SelectedPath))
        {
            DevToolsMessage.Warning("Por favor, selecione um diretório de origem.", "Erro de Validação");
            return;
        }

        if (!System.IO.Directory.Exists(SourcePathSelector.SelectedPath))
        {
            DevToolsMessage.Error("O diretório de origem especificado não existe.", "Diretório Inválido");
            return;
        }

        if (string.IsNullOrWhiteSpace(OutputPathSelector.SelectedPath))
        {
            DevToolsMessage.Warning("Por favor, selecione um diretório de destino.", "Erro de Validação");
            return;
        }

        if (!System.IO.Directory.Exists(OutputPathSelector.SelectedPath))
        {
            DevToolsMessage.Error("O diretório de destino especificado não existe.", "Diretório Inválido");
            return;
        }

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
            RootPath: SourcePathSelector.SelectedPath,
            OutputPath: OutputPathSelector.SelectedPath,
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
             AppLogger.Error("Erro crítico ao executar Harvest", ex);
             DevToolsMessage.Error($"Erro crítico ao executar: {ex.Message}", "Erro");
        }
        finally
        {
            IsEnabled = true;
        }
    }
    
    private void Header_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

