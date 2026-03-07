using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using DevTools.Core.Configuration;
using DevTools.Core.Models;
using DevTools.Organizer.Engine;
using DevTools.Organizer.Models;
using DevTools.Presentation.Wpf.Services;

namespace DevTools.Presentation.Wpf.Views;

public partial class OrganizerWindow : Window
{
    private readonly JobManager _jobManager;
    private readonly SettingsService _settingsService;
    private readonly ToolConfigurationManager _toolConfigurationManager;

    public OrganizerWindow(JobManager jobManager, SettingsService settingsService, ToolConfigurationManager toolConfigurationManager)
    {
        InitializeComponent();
        _jobManager = jobManager;
        _settingsService = settingsService;
        _toolConfigurationManager = toolConfigurationManager;

        Deactivated += (s, e) =>
        {
            // Mantido aberto para evitar fechamento acidental.
        };

        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplyDefaultConfiguration();

        if (!string.IsNullOrWhiteSpace(_settingsService.Settings.LastOrganizerInputPath))
            InputPathSelector.SelectedPath = _settingsService.Settings.LastOrganizerInputPath;
    }

    private void ApplyDefaultConfiguration()
    {
        var configuration = _toolConfigurationManager.GetDefaultConfiguration("Organizer");
        if (configuration == null)
            return;

        if (configuration.Options.TryGetValue("input-path", out var inputPath) && !string.IsNullOrWhiteSpace(inputPath))
            InputPathSelector.SelectedPath = inputPath;

        if (configuration.Options.TryGetValue("output-path", out var outputPath) && !string.IsNullOrWhiteSpace(outputPath))
            OutputPathSelector.SelectedPath = outputPath;

        if (configuration.Options.TryGetValue("simulate", out var simulate) && bool.TryParse(simulate, out var parsedSimulate))
            SimulateCheck.IsChecked = parsedSimulate;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void RunButton_Click(object sender, RoutedEventArgs e)
    {
        var inputPath = InputPathSelector.SelectedPath;
        var outputPath = string.IsNullOrWhiteSpace(OutputPathSelector.SelectedPath)
            ? null
            : OutputPathSelector.SelectedPath;
        var simulate = SimulateCheck.IsChecked ?? false;

        if (!ValidateInputs(out var validationError))
        {
            ValidationUiService.ShowInline(MainFrame, validationError);
            return;
        }

        ValidationUiService.ClearInline(MainFrame);

        _settingsService.Settings.LastOrganizerInputPath = inputPath;
        _settingsService.Save();

        Close();

        _jobManager.StartJob("Organizer", async (reporter, ct) =>
        {
            var engine = new OrganizerEngine();
            var request = new OrganizerRequest(inputPath, outputPath, null, null, !simulate);

            var result = await engine.ExecuteAsync(request, reporter, ct);
            return result.IsSuccess
                ? $"Organizacao concluida! Arquivos processados: {result.Value?.Stats.TotalFiles ?? 0}"
                : $"Falha na organizacao: {string.Join(", ", result.Errors.Select(e => e.Message))}";
        });
    }

    private bool ValidateInputs(out string errorMessage)
    {
        return ValidationUiService.ValidateRequiredFields(
            out errorMessage,
            ValidationUiService.RequiredPath("Pasta de Entrada", InputPathSelector, InputPathSelector.SelectedPath));
    }
}

