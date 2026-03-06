using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using DevTools.Core.Models;
using DevTools.Organizer.Engine;
using DevTools.Organizer.Models;
using DevTools.Presentation.Wpf.Services;

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

        Deactivated += (s, e) =>
        {
            // Mantido aberto para evitar fechamento acidental.
        };
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
        if (string.IsNullOrWhiteSpace(InputPathSelector.SelectedPath))
        {
            errorMessage = "Os campos abaixo nao podem ficar em branco:\n- Pasta de Entrada";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }
}

