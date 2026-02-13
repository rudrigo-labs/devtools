using System;
using System.Linq;
using System.Windows;
using DevTools.Harvest.Engine;
using DevTools.Harvest.Models;
using DevTools.Presentation.Wpf.Services;

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

    private void Run_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(SourcePathSelector.SelectedPath))
        {
            MessageBox.Show("Por favor, selecione um diretório de origem.", "Erro de Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(OutputPathSelector.SelectedPath))
        {
            MessageBox.Show("Por favor, selecione um diretório de destino.", "Erro de Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
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

        Close();

        // (opcional) abrir automaticamente o Job Center ao iniciar
        // _jobManager.ShowJobCenter(); // Not needed if TrayService handles it or if we just start the job

        _jobManager.StartJob("Harvest", async (p, ct) =>
        {
            var engine = new HarvestEngine();
            var result = await engine.ExecuteAsync(Result, p, ct);

            return result.IsSuccess
                ? $"Harvest concluído! {result.Value!.Report.Hits.Count} arquivos encontrados."
                : $"Falha no Harvest: {string.Join(", ", result.Errors.Select(x => x.Message))}";
        });
    }
    
    private void Header_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // DragMove();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
