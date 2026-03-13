using System.Windows;
using DevTools.Host.Wpf.Facades;
using DevTools.Host.Wpf.Services;
using DevTools.Organizer.Models;

namespace DevTools.Host.Wpf.Views;

public partial class OrganizerWorkspaceView : System.Windows.Controls.UserControl
{
    private readonly IOrganizerFacade _facade;
    private CancellationTokenSource? _executionCts;
    private bool _isExecuting;

    public OrganizerWorkspaceView(IOrganizerFacade facade)
    {
        _facade = facade;
        InitializeComponent();
        ApplyModeState();
    }

    private async void ActionExecute_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteAsync().ConfigureAwait(true);
    }

    private void ActionCancel_Click(object sender, RoutedEventArgs e)
    {
        if (_isExecuting)
        {
            _executionCts?.Cancel();
            ExecutionStatusText.Text = "Cancelando...";
        }
    }

    private async Task ExecuteAsync()
    {
        if (_isExecuting) return;

        ValidationUiService.SetPathSelectorInvalid(InboxPathSelector, false);

        if (!ValidationUiService.ValidateRequiredFields(
            out var errorMessage,
            ValidationUiService.RequiredPath("Pasta de entrada", InboxPathSelector, InboxPathSelector.SelectedPath)))
        {
            ValidationUiService.ShowInline(ExecutionStatusText, errorMessage);
            return;
        }

        var request = BuildRequest();
        var apply = request.Apply;

        _executionCts?.Dispose();
        _executionCts = new CancellationTokenSource();
        _isExecuting = true;
        ResultPanel.Visibility = Visibility.Collapsed;
        ApplyModeState();
        ExecutionStatusText.Text = apply ? "Organizando arquivos..." : "Simulando organização...";

        try
        {
            var result = await _facade.ExecuteAsync(request, _executionCts.Token).ConfigureAwait(true);

            if (!result.IsSuccess)
            {
                ValidationUiService.ShowInline(ExecutionStatusText,
                    string.Join(" | ", result.Errors.Select(x => x.Message)));
                return;
            }

            ValidationUiService.ClearInline(ExecutionStatusText);
            var data = result.Value!;
            var s = data.Stats;

            ResultSummaryText.Text =
                $"Total: {s.TotalFiles} | Elegíveis: {s.EligibleFiles} | " +
                $"Movidos: {s.WouldMove} | Duplicatas: {s.Duplicates} | " +
                $"Ignorados: {s.Ignored} | Erros: {s.Errors}";

            ResultsList.ItemsSource = data.Plan
                .Where(p => p.Action is OrganizerAction.WouldMove or OrganizerAction.Moved or OrganizerAction.Duplicate)
                .ToList();

            ResultPanel.Visibility = Visibility.Visible;

            var mode = apply ? "Executado" : "Simulação";
            ExecutionStatusText.Text = $"{mode} concluído. {s.WouldMove} arquivo(s) classificado(s).";
        }
        catch (OperationCanceledException)
        {
            ValidationUiService.ClearInline(ExecutionStatusText);
            ExecutionStatusText.Text = "Operação cancelada.";
        }
        finally
        {
            _isExecuting = false;
            _executionCts?.Dispose();
            _executionCts = null;
            ApplyModeState();
        }
    }

    private OrganizerRequest BuildRequest() => new()
    {
        InboxPath  = InboxPathSelector.SelectedPath?.Trim() ?? string.Empty,
        OutputPath = OutputPathSelector.SelectedPath?.Trim() ?? string.Empty,
        MinScore   = int.TryParse(MinScoreInput.Text.Trim(), out var ms) ? Math.Max(0, ms) : 3,
        Apply      = ApplyCheck.IsChecked ?? false
    };

    private void ApplyModeState()
    {
        Actions.Visibility = _isExecuting ? Visibility.Visible : Visibility.Collapsed;
        Actions.ShowSave = false;
        Actions.CanSave = false;
        Actions.ShowHelp = false;
        Actions.CanCancel = _isExecuting;
        Actions.ShowCancel = _isExecuting;
        Actions.CancelText = "Cancelar";
    }
}
