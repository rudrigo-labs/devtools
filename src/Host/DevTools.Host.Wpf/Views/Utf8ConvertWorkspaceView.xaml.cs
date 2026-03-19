using System.Windows;
using DevTools.Core.Models;
using DevTools.Host.Wpf.Facades;
using DevTools.Host.Wpf.Services;
using DevTools.Utf8Convert.Models;

namespace DevTools.Host.Wpf.Views;

public partial class Utf8ConvertWorkspaceView : System.Windows.Controls.UserControl
{
    private const string ToolHistorySlug = "utf8_convert";
    private const string ToolDisplayName = "UTF-8 Convert";
    private readonly AppSettings _appSettings;
    private readonly IUtf8ConvertFacade _facade;
    private CancellationTokenSource? _executionCts;
    private bool _isExecuting;
    private bool _defaultsApplied;

    public Utf8ConvertWorkspaceView(IUtf8ConvertFacade facade, AppSettings appSettings)
    {
        _facade = facade;
        _appSettings = appSettings;
        InitializeComponent();
        Loaded += Utf8ConvertWorkspaceView_Loaded;
        ApplyModeState();
    }

    private void Utf8ConvertWorkspaceView_Loaded(object sender, RoutedEventArgs e)
    {
        if (_defaultsApplied)
            return;

        _defaultsApplied = true;
        IncludeGlobsInput.Text = string.Join(Environment.NewLine, _appSettings.FileTools.DefaultIncludeGlobs);
        ExcludeGlobsInput.Text = string.Join(Environment.NewLine, _appSettings.FileTools.DefaultExcludeGlobs);
    }

    private async void ActionExecute_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteAsync().ConfigureAwait(true);
    }

    private async void HistoryButton_Click(object sender, RoutedEventArgs e)
        => await ToolHistoryViewHelper.ShowAndApplyAsync(WorkspaceRoot, ToolHistorySlug, ToolDisplayName, ExecutionStatusText).ConfigureAwait(true);

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

        ValidationUiService.SetPathSelectorInvalid(RootPathSelector, false);

        if (!ValidationUiService.ValidateRequiredFields(
            out var errorMessage,
            ValidationUiService.RequiredPath("Pasta raiz", RootPathSelector, RootPathSelector.SelectedPath)))
        {
            ValidationUiService.ShowInline(ExecutionStatusText, errorMessage);
            return;
        }

        var request = BuildRequest();
        var dryRun = request.DryRun;
        await ToolHistoryViewHelper.RecordAsync(ToolHistorySlug, WorkspaceRoot, "Executar conversão UTF-8").ConfigureAwait(true);

        _executionCts?.Dispose();
        _executionCts = new CancellationTokenSource();
        _isExecuting = true;
        ResultPanel.Visibility = Visibility.Collapsed;
        ApplyModeState();
        ExecutionStatusText.Text = dryRun ? "Simulando..." : "Convertendo arquivos...";

        try
        {
            var result = await _facade.ExecuteAsync(request, _executionCts.Token).ConfigureAwait(true);

            ValidationUiService.ClearInline(ExecutionStatusText);

            var data = result.Value;
            if (data is not null)
            {
                var s = data.Summary;
                ResultSummaryText.Text =
                    $"Escaneados: {s.FilesScanned} | " +
                    $"Convertidos: {s.Converted} | " +
                    $"Já UTF-8: {s.AlreadyUtf8} | " +
                    $"Binários: {s.SkippedBinary} | " +
                    $"Excluídos: {s.SkippedExcluded} | " +
                    $"Erros: {s.Errors}";
                ResultPanel.Visibility = Visibility.Visible;
            }

            if (!result.IsSuccess)
            {
                ValidationUiService.ShowInline(ExecutionStatusText,
                    string.Join(" | ", result.Errors.Select(x => x.Message)));
                return;
            }

            var mode = dryRun ? "Simulação concluída" : "Conversão concluída";
            ExecutionStatusText.Text = $"{mode}. {data?.Summary.Converted ?? 0} arquivo(s) convertido(s).";
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

    private Utf8ConvertRequest BuildRequest()
    {
        var includeGlobs = ParseLines(IncludeGlobsInput.Text) ?? _appSettings.FileTools.DefaultIncludeGlobs;
        var excludeGlobs = ParseLines(ExcludeGlobsInput.Text) ?? _appSettings.FileTools.DefaultExcludeGlobs;

        return new Utf8ConvertRequest
        {
            RootPath     = RootPathSelector.SelectedPath?.Trim() ?? string.Empty,
            Recursive    = RecursiveCheck.IsChecked ?? true,
            OutputBom    = OutputBomCheck.IsChecked ?? true,
            CreateBackup = CreateBackupCheck.IsChecked ?? true,
            DryRun       = DryRunCheck.IsChecked ?? false,
            IncludeGlobs = includeGlobs,
            ExcludeGlobs = excludeGlobs
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
        Actions.HelpContextKey = "utf8convert:execution";
        Actions.ShowNew = false;
        Actions.ShowDelete = false;
        Actions.ShowCancel = false;
        Actions.ShowSave = true;
        Actions.SaveText = "Executar";
        Actions.SaveIconKind = "Play";
        Actions.CanSave = !_isExecuting;
        Actions.ShowBack = true;
        Actions.BackText = _isExecuting ? "Cancelar" : "Voltar";
        Actions.BackIconKind = _isExecuting ? "CloseCircleOutline" : "ArrowLeft";
        Actions.CanBack = true;
    }
}
