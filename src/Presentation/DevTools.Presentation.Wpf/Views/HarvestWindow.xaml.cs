using System;
using System.Linq;
using System.Windows;
using System.Collections.Generic;
using DevTools.Harvest.Engine;
using DevTools.Harvest.Models;
using DevTools.Presentation.Wpf.Services;
using DevTools.Core.Models;

using DevTools.Core.Configuration;

namespace DevTools.Presentation.Wpf.Views;

public partial class HarvestWindow : Window
{
    private readonly JobManager _jobManager = null!;
    private readonly SettingsService _settingsService = null!;
    private readonly ProfileManager _profileManager = null!;
    private ToolProfile? _currentProfile;
    public HarvestRequest? Result { get; private set; }

    public HarvestWindow(JobManager jobManager, SettingsService settingsService, ProfileManager profileManager)
    {
        InitializeComponent();
        _jobManager = jobManager;
        _settingsService = settingsService;
        _profileManager = profileManager;

        Loaded += OnLoaded;
    }

    // Construtor para o Designer
    public HarvestWindow()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Tentar carregar perfil padrão
        _currentProfile = _profileManager?.GetDefaultProfile("Harvest");
        if (_currentProfile != null)
        {
            if (_currentProfile.Options.TryGetValue("source-path", out var src)) SourcePathSelector.SelectedPath = src;
            if (_currentProfile.Options.TryGetValue("output-path", out var outPath)) OutputPathSelector.SelectedPath = outPath;
            if (_currentProfile.Options.TryGetValue("min-score", out var score)) MinScoreBox.Text = score;
        }
        else
        {
            // Fallback para configurações salvas anteriormente (comportamento original)
            if (!string.IsNullOrEmpty(_settingsService?.Settings.LastHarvestSourcePath))
                SourcePathSelector.SelectedPath = _settingsService.Settings.LastHarvestSourcePath;
                
            if (!string.IsNullOrEmpty(_settingsService?.Settings.LastHarvestOutputPath))
                OutputPathSelector.SelectedPath = _settingsService.Settings.LastHarvestOutputPath;

            if (!string.IsNullOrEmpty(_settingsService?.Settings.LastHarvestConfigPath))
                ConfigPathSelector.SelectedPath = _settingsService.Settings.LastHarvestConfigPath;

            if (_settingsService?.Settings.LastHarvestMinScore.HasValue == true)
                MinScoreBox.Text = _settingsService.Settings.LastHarvestMinScore.Value.ToString();

            if (_settingsService?.Settings.LastHarvestCopyFiles.HasValue == true)
                CopyFilesCheck.IsChecked = _settingsService.Settings.LastHarvestCopyFiles.Value;
        }
    }

    private bool ValidateInputs(out string errorMessage)
    {
        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(SourcePathSelector.SelectedPath))
            missing.Add("Diretório de Origem");

        if (string.IsNullOrWhiteSpace(OutputPathSelector.SelectedPath))
            missing.Add("Diretório de Destino");

        if (missing.Count > 0)
        {
            errorMessage = "Os campos abaixo não podem ficar em branco:\n- " + string.Join("\n- ", missing);
            return false;
        }

        if (string.IsNullOrWhiteSpace(SourcePathSelector.SelectedPath))
        {
            errorMessage = "Por favor, selecione um diretório de origem.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(OutputPathSelector.SelectedPath))
        {
            errorMessage = "Por favor, selecione um diretório de destino.";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(MinScoreBox.Text) && !int.TryParse(MinScoreBox.Text, out _))
        {
            errorMessage = "Score mínimo deve ser um número inteiro válido.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    private async void Run_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateInputs(out var validationError))
        {
            UiMessageService.ShowError(validationError, "Erro de Validação");
            return;
        }

        // Salvar configurações apenas se opt-in
        if (RememberSettingsCheck.IsChecked == true)
        {
            _settingsService.Settings.LastHarvestSourcePath = SourcePathSelector.SelectedPath;
            _settingsService.Settings.LastHarvestOutputPath = OutputPathSelector.SelectedPath;
            _settingsService.Settings.LastHarvestConfigPath = ConfigPathSelector.SelectedPath;
            
            if (int.TryParse(MinScoreBox.Text, out var minScore))
                _settingsService.Settings.LastHarvestMinScore = minScore;

            _settingsService.Settings.LastHarvestCopyFiles = CopyFilesCheck.IsChecked;
            _settingsService.Save();

            // Sincronizar com o perfil padrão se estiver em uso
            if (_currentProfile != null)
            {
                _currentProfile.Options["source-path"] = SourcePathSelector.SelectedPath ?? "";
                _currentProfile.Options["output-path"] = OutputPathSelector.SelectedPath ?? "";
                _currentProfile.Options["min-score"] = MinScoreBox.Text ?? "";
                _profileManager.SaveProfile("Harvest", _currentProfile);
            }
        }

        Result = new HarvestRequest(
            RootPath: SourcePathSelector.SelectedPath,
            OutputPath: OutputPathSelector.SelectedPath,
            ConfigPath: ConfigPathSelector.SelectedPath,
            MinScore: int.TryParse(MinScoreBox.Text, out var ms) ? ms : 0,
            CopyFiles: CopyFilesCheck.IsChecked ?? true
        );

        // Execute directly to show result in-place
        var engine = new HarvestEngine();
        
        // Disable UI while running
        IsEnabled = false;
        RunSummary.Clear();
        
        try 
        {
            var result = await System.Threading.Tasks.Task.Run(() => engine.ExecuteAsync(Result));
            
            RunSummary.BindResult(result);
        }
        catch (Exception ex)
        {
            UiMessageService.ShowError("Erro crítico ao executar coleta.", "Erro na ferramenta Harvest", ex);
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
