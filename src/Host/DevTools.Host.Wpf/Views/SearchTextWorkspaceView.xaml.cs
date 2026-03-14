using System.Windows;
using DevTools.Host.Wpf.Facades;
using DevTools.Host.Wpf.Services;
using DevTools.SearchText.Models;

namespace DevTools.Host.Wpf.Views;

public partial class SearchTextWorkspaceView : System.Windows.Controls.UserControl
{
    private readonly ISearchTextFacade _facade;
    private CancellationTokenSource? _executionCts;
    private bool _isExecuting;

    public SearchTextWorkspaceView(ISearchTextFacade facade)
    {
        _facade = facade;
        InitializeComponent();
        ApplyModeState();
    }

    private async void ActionExecute_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteAsync().ConfigureAwait(true);
    }

    private void ActionBack_Click(object sender, RoutedEventArgs e)
    {
        if (_isExecuting)
        {
            _executionCts?.Cancel();
            ExecutionStatusText.Text = "Cancelando...";
            return;
        }

        if (Window.GetWindow(this) is MainWindow mainWindow)
            mainWindow.OpenFerramentasHome();
    }

    private async Task ExecuteAsync()
    {
        if (_isExecuting) return;

        ValidationUiService.SetControlInvalid(PatternInput, false);
        ValidationUiService.SetPathSelectorInvalid(RootPathSelector, false);

        if (!ValidationUiService.ValidateRequiredFields(
            out var errorMessage,
            ValidationUiService.RequiredPath("Pasta raiz", RootPathSelector, RootPathSelector.SelectedPath),
            ValidationUiService.RequiredControl("Padrão de busca", PatternInput, PatternInput.Text)))
        {
            ValidationUiService.ShowInline(ExecutionStatusText, errorMessage);
            return;
        }

        var request = BuildRequest();

        _executionCts?.Dispose();
        _executionCts = new CancellationTokenSource();
        _isExecuting = true;
        ResultsList.Visibility = Visibility.Collapsed;
        ResultSummaryText.Visibility = Visibility.Collapsed;
        ApplyModeState();
        ExecutionStatusText.Text = "Buscando...";

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

            ResultSummaryText.Text =
                $"Arquivos escaneados: {data.TotalFilesScanned} | " +
                $"Com ocorrências: {data.TotalFilesWithMatches} | " +
                $"Total de ocorrências: {data.TotalOccurrences}";
            ResultSummaryText.Visibility = Visibility.Visible;

            if (data.Files.Count > 0)
            {
                ResultsList.ItemsSource = data.Files;
                ResultsList.Visibility = Visibility.Visible;
            }

            ExecutionStatusText.Text = data.TotalFilesWithMatches == 0
                ? "Nenhuma ocorrência encontrada."
                : $"Concluído. {data.TotalFilesWithMatches} arquivo(s) com ocorrências.";
        }
        catch (OperationCanceledException)
        {
            ValidationUiService.ClearInline(ExecutionStatusText);
            ExecutionStatusText.Text = "Busca cancelada.";
        }
        finally
        {
            _isExecuting = false;
            _executionCts?.Dispose();
            _executionCts = null;
            ApplyModeState();
        }
    }

    private SearchTextRequest BuildRequest()
    {
        return new SearchTextRequest
        {
            RootPath       = RootPathSelector.SelectedPath?.Trim() ?? string.Empty,
            Pattern        = PatternInput.Text.Trim(),
            UseRegex       = UseRegexCheck.IsChecked ?? false,
            CaseSensitive  = CaseSensitiveCheck.IsChecked ?? false,
            WholeWord      = WholeWordCheck.IsChecked ?? false,
            SkipBinaryFiles = SkipBinaryCheck.IsChecked ?? true,
            MaxFileSizeKb  = int.TryParse(MaxFileSizeInput.Text.Trim(), out var sz) && sz > 0 ? sz : null,
            MaxMatchesPerFile = int.TryParse(MaxMatchesInput.Text.Trim(), out var mm) ? Math.Max(0, mm) : 0,
            IncludeGlobs   = ParseLines(IncludeGlobsInput.Text),
            ExcludeGlobs   = ParseLines(ExcludeGlobsInput.Text),
            ReturnLines    = true
        };
    }

    private static IReadOnlyList<string>? ParseLines(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var lines = text
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();
        return lines.Count > 0 ? lines : null;
    }

    private void ApplyModeState()
    {
        Actions.Visibility = Visibility.Visible;
        Actions.ShowHelp = true;
        Actions.ShowNew = false;
        Actions.ShowDelete = false;
        Actions.ShowCancel = false;
        Actions.ShowSave = true;
        Actions.SaveText = "Buscar";
        Actions.SaveIconKind = "Play";
        Actions.CanSave = !_isExecuting;
        Actions.ShowBack = true;
        Actions.BackText = _isExecuting ? "Cancelar" : "Voltar";
        Actions.BackIconKind = _isExecuting ? "CloseCircleOutline" : "ArrowLeft";
        Actions.CanBack = true;
    }
}
