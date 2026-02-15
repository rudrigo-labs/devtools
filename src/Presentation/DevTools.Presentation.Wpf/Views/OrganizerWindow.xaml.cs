using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Collections.Generic;
using DevTools.Organizer.Engine;
using DevTools.Organizer.Models;
using DevTools.Presentation.Wpf.Services;
using DevTools.Core.Models;

namespace DevTools.Presentation.Wpf.Views;

public partial class OrganizerWindow : Window
{
    private readonly JobManager _jobManager;
    private readonly SettingsService _settingsService;

    public OrganizerWindow(JobManager jobManager, SettingsService settingsService)
    {
        InitializeComponent();
        _jobManager = jobManager;
        _settingsService = settingsService;

        ProfileSelector.GetOptionsFunc = GetCurrentOptions;
        ProfileSelector.ProfileLoaded += LoadProfile;
        
        // Carregar configurações
        if (!string.IsNullOrEmpty(_settingsService.Settings.LastOrganizerInputPath))
            InputPathSelector.SelectedPath = _settingsService.Settings.LastOrganizerInputPath;
        else
            InputPathSelector.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        // Posicionar no canto inferior direito
        Loaded += (s, e) => 
        {
            var desktopWorkingArea = SystemParameters.WorkArea;
            this.Left = desktopWorkingArea.Right - this.ActualWidth - 20;
            this.Top = desktopWorkingArea.Bottom - this.ActualHeight - 20;
            this.Activate();
        };

        // Comportamento estilo Tray: Fechar ao clicar fora
        this.Deactivated += (s, e) => 
        {
             // this.Close(); 
             // Comentado para evitar fechamento acidental ao usar dialogs
        };
    }

    private Dictionary<string, string> GetCurrentOptions()
    {
        var options = new Dictionary<string, string>();
        options["inbox"] = InputPathSelector.SelectedPath;
        options["output"] = OutputPathSelector.SelectedPath;
        options["apply"] = (!(SimulateCheck.IsChecked ?? false)).ToString().ToLowerInvariant();
        return options;
    }

    private void LoadProfile(ToolProfile profile)
    {
        if (profile.Options.TryGetValue("inbox", out var inbox)) InputPathSelector.SelectedPath = inbox;
        else if (profile.Options.TryGetValue("input", out var input)) InputPathSelector.SelectedPath = input;
        
        if (profile.Options.TryGetValue("output", out var output)) OutputPathSelector.SelectedPath = output;
        
        if (profile.Options.TryGetValue("apply", out var applyStr))
             SimulateCheck.IsChecked = !(bool.TryParse(applyStr, out var a) ? a : false);
    }

    private void Header_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // Dragging disabled per user request
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void RunButton_Click(object sender, RoutedEventArgs e)
    {
        var inputPath = InputPathSelector.SelectedPath;
        var outputPath = OutputPathSelector.SelectedPath;
        var simulate = SimulateCheck.IsChecked ?? false;

        if (string.IsNullOrWhiteSpace(inputPath))
        {
            System.Windows.MessageBox.Show("Por favor, selecione uma pasta de entrada.", "Erro", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Salvar configurações
        _settingsService.Settings.LastOrganizerInputPath = inputPath;
        _settingsService.Save();

        // Se output vazio, usa input (comportamento padrão do Organizer)
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            outputPath = inputPath;
        }

        // Fecha janela para iniciar background job
        Close();

        // Inicia Job
        _jobManager.StartJob("Organizer", async (reporter, ct) =>
        {
            var engine = new OrganizerEngine();
            // Apply = !simulate
            var request = new OrganizerRequest(inputPath, outputPath, null, null, !simulate);
            
            var result = await engine.ExecuteAsync(request, reporter, ct);
            return result.IsSuccess 
                ? $"Organização concluída! Arquivos processados: {result.Value?.Stats.TotalFiles ?? 0}" 
                : $"Falha na organização: {string.Join(", ", result.Errors.Select(e => e.Message))}";
        });
    }
}
