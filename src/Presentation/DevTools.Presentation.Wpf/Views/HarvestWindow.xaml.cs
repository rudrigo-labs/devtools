using System;
using System.Linq;
using System.Windows;
using System.Collections.Generic;
using DevTools.Harvest.Engine;
using DevTools.Harvest.Models;
using DevTools.Presentation.Wpf.Services;
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

        ProfileSelector.GetOptionsFunc = GetCurrentOptions;
        ProfileSelector.ProfileLoaded += LoadProfile;

        // Carregar configurações salvas
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
        
        // Posicionar no canto inferior direito e fechar ao perder foco
        /* Position handled by TrayService
        Loaded += (s, e) => 
        {
            var desktopWorkingArea = SystemParameters.WorkArea;
            this.Left = desktopWorkingArea.Right - this.ActualWidth - 20;
            this.Top = desktopWorkingArea.Bottom - this.ActualHeight - 20;
            this.Activate();
        };
        */

        // Comportamento estilo Tray: Fechar ao clicar fora
        this.Deactivated += (s, e) => 
        {
            // Se o usuário clicar fora, fecha a janela (como um menu)
            // Mas apenas se não estiver abrindo um diálogo filho (como o PathSelector)
            // TODO: Refinar lógica se PathSelector causar Deactivate. Por enquanto, simplificado.
            // this.Close(); 
            // Comentado pois o FolderBrowserDialog causa Deactivate. Precisamos de flag.
        };
    }

    private Dictionary<string, string> GetCurrentOptions()
    {
        var options = new Dictionary<string, string>();
        options["root"] = SourcePathSelector.SelectedPath;
        options["output"] = OutputPathSelector.SelectedPath;
        options["config"] = ConfigPathSelector.SelectedPath;
        options["min-score"] = MinScoreBox.Text;
        options["copy"] = (CopyFilesCheck.IsChecked ?? true).ToString().ToLowerInvariant();
        return options;
    }

    private void LoadProfile(ToolProfile profile)
    {
        if (profile.Options.TryGetValue("root", out var root)) SourcePathSelector.SelectedPath = root;
        if (profile.Options.TryGetValue("output", out var output)) OutputPathSelector.SelectedPath = output;
        if (profile.Options.TryGetValue("config", out var config)) ConfigPathSelector.SelectedPath = config;
        
        if (profile.Options.TryGetValue("min-score", out var minScore))
             MinScoreBox.Text = minScore;
             
        if (profile.Options.TryGetValue("copy", out var copy))
             CopyFilesCheck.IsChecked = bool.TryParse(copy, out var c) ? c : true;
    }

    private bool ValidateInputs(out string errorMessage)
    {
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
