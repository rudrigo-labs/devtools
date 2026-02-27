using System.Windows;
using DevTools.Utf8Convert.Engine;
using DevTools.Utf8Convert.Models;
using DevTools.Presentation.Wpf.Services;
using DevTools.Presentation.Wpf.Utilities;
using DevTools.Core.Models;

namespace DevTools.Presentation.Wpf.Views;

public partial class Utf8Window : Window
{
    private readonly JobManager _jobManager;
    private readonly SettingsService _settingsService;

    public Utf8ConvertRequest? Result { get; private set; }

    public Utf8Window(JobManager jobManager, SettingsService settingsService)
    {
        InitializeComponent();
        _jobManager = jobManager;
        _settingsService = settingsService;

        // Load Settings
        if (!string.IsNullOrEmpty(_settingsService.Settings.LastUtf8RootPath))
            RootPathSelector.SelectedPath = _settingsService.Settings.LastUtf8RootPath;
        
        if (_settingsService.Settings.LastUtf8Recursive.HasValue)
            RecursiveCheck.IsChecked = _settingsService.Settings.LastUtf8Recursive.Value;

        if (_settingsService.Settings.LastUtf8DryRun.HasValue)
            DryRunCheck.IsChecked = _settingsService.Settings.LastUtf8DryRun.Value;

        if (_settingsService.Settings.LastUtf8Backup.HasValue)
            BackupCheck.IsChecked = _settingsService.Settings.LastUtf8Backup.Value;

        if (_settingsService.Settings.LastUtf8Bom.HasValue)
            BomCheck.IsChecked = _settingsService.Settings.LastUtf8Bom.Value;

        if (!string.IsNullOrEmpty(_settingsService.Settings.LastUtf8Includes))
            IncludeBox.Text = _settingsService.Settings.LastUtf8Includes;

        if (!string.IsNullOrEmpty(_settingsService.Settings.LastUtf8Excludes))
            ExcludeBox.Text = _settingsService.Settings.LastUtf8Excludes;

        // Position handled by TrayService logic
    }

    private async void Run_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(RootPathSelector.SelectedPath))
        {
            DevToolsMessage.Warning("Por favor, selecione um diretório raiz.", "Erro de Validação");
            return;
        }

        if (!System.IO.Directory.Exists(RootPathSelector.SelectedPath))
        {
            DevToolsMessage.Error("O diretório raiz especificado não existe.", "Diretório Inválido");
            return;
        }

        if (RememberSettingsCheck.IsChecked == true)
        {
            _settingsService.Settings.LastUtf8RootPath = RootPathSelector.SelectedPath;
            _settingsService.Settings.LastUtf8Recursive = RecursiveCheck.IsChecked;
            _settingsService.Settings.LastUtf8DryRun = DryRunCheck.IsChecked;
            _settingsService.Settings.LastUtf8Backup = BackupCheck.IsChecked;
            _settingsService.Settings.LastUtf8Bom = BomCheck.IsChecked;
            _settingsService.Settings.LastUtf8Includes = IncludeBox.Text;
            _settingsService.Settings.LastUtf8Excludes = ExcludeBox.Text;
            _settingsService.Save();
        }

        var includes = string.IsNullOrWhiteSpace(IncludeBox.Text) 
            ? null 
            : IncludeBox.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();

        var excludes = string.IsNullOrWhiteSpace(ExcludeBox.Text) 
            ? null 
            : ExcludeBox.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();

        Result = new Utf8ConvertRequest(
            RootPath: RootPathSelector.SelectedPath,
            Recursive: RecursiveCheck.IsChecked ?? true,
            DryRun: DryRunCheck.IsChecked ?? false,
            CreateBackup: BackupCheck.IsChecked ?? true,
            OutputBom: BomCheck.IsChecked ?? true,
            IncludeGlobs: includes,
            ExcludeGlobs: excludes
        );

        var engine = new Utf8ConvertEngine();
        
        IsEnabled = false;
        RunSummary.Clear();

        try
        {
            var result = await Task.Run(() => engine.ExecuteAsync(Result));
            RunSummary.BindResult(result);
        }
        catch (Exception ex)
        {
            AppLogger.Error("Erro crítico ao executar Utf8Convert", ex);
            DevToolsMessage.Error($"Erro crítico: {ex.Message}", "Erro");
        }
        finally
        {
            IsEnabled = true;
        }
    }

    private void Header_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        DragMove();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
