using System.Windows;
using DevTools.Host.Wpf.Facades;
using DevTools.Host.Wpf.Services;
using DevTools.Image.Models;

namespace DevTools.Host.Wpf.Views;

public partial class ImageSplitWorkspaceView : System.Windows.Controls.UserControl
{
    private const string ToolHistorySlug = "image_split";
    private const string ToolDisplayName = "Image Split";
    private readonly IImageSplitFacade _facade;
    private CancellationTokenSource? _executionCts;
    private bool _isExecuting;

    public ImageSplitWorkspaceView(IImageSplitFacade facade)
    {
        _facade = facade;
        InitializeComponent();
        ApplyModeState();
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
            ExecutionStatusText.Text = "Cancelando execução...";
            return;
        }

        if (Window.GetWindow(this) is MainWindow mainWindow)
            mainWindow.OpenFerramentasHome();
    }

    private async Task ExecuteAsync()
    {
        if (_isExecuting)
            return;

        ClearInlineValidationStates();

        if (!ValidationUiService.ValidateRequiredFields(
            out var errorMessage,
            ValidationUiService.RequiredPath("Arquivo de imagem", InputPathSelector, InputPathSelector.SelectedPath),
            ValidationUiService.RequiredPath("Pasta de saÃ­da", OutputDirectorySelector, OutputDirectorySelector.SelectedPath)))
        {
            ValidationUiService.ShowInline(ExecutionStatusText, errorMessage);
            return;
        }

        var request = BuildRequest();
        await ToolHistoryViewHelper.RecordAsync(ToolHistorySlug, WorkspaceRoot, "Executar corte de imagem").ConfigureAwait(true);

        _executionCts?.Dispose();
        _executionCts = new CancellationTokenSource();
        _isExecuting = true;
        ApplyModeState();
        ExecutionStatusText.Text = "Executando...";

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

            if (data.TotalComponents == 0)
            {
                ExecutionStatusText.Text = "Nenhum componente encontrado. Verifique se a imagem possui fundo transparente.";
                return;
            }

            ExecutionStatusText.Text = $"ConcluÃ­do. {data.Outputs.Count} arquivo(s) gerado(s) em {data.OutputDirectory}";
        }
        catch (OperationCanceledException)
        {
            ValidationUiService.ClearInline(ExecutionStatusText);
            ExecutionStatusText.Text = "ExecuÃ§Ã£o cancelada.";
        }
        finally
        {
            _isExecuting = false;
            _executionCts?.Dispose();
            _executionCts = null;
            ApplyModeState();
        }
    }

    private ImageSplitRequest BuildRequest()
    {
        var ext = OutputExtensionInput.Text.Trim();
        if (!string.IsNullOrWhiteSpace(ext) && !ext.StartsWith('.'))
            ext = "." + ext;

        return new ImageSplitRequest
        {
            InputPath = InputPathSelector.SelectedPath?.Trim() ?? string.Empty,
            OutputDirectory = OutputDirectorySelector.SelectedPath?.Trim() ?? string.Empty,
            OutputBaseName = OutputBaseNameInput.Text.Trim(),
            OutputExtension = ext,
            StartIndex = int.TryParse(StartIndexInput.Text.Trim(), out var si) ? Math.Max(1, si) : 1,
            AlphaThreshold = byte.TryParse(AlphaThresholdInput.Text.Trim(), out var at) ? at : (byte)10,
            MinRegionWidth = int.TryParse(MinWidthInput.Text.Trim(), out var mw) ? Math.Max(1, mw) : 3,
            MinRegionHeight = int.TryParse(MinHeightInput.Text.Trim(), out var mh) ? Math.Max(1, mh) : 3,
            Overwrite = OverwriteCheck.IsChecked ?? false
        };
    }

    private void ApplyModeState()
    {
        Actions.ShowHelp = true;
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

    private void ClearInlineValidationStates()
    {
        ValidationUiService.SetPathSelectorInvalid(InputPathSelector, false);
        ValidationUiService.SetPathSelectorInvalid(OutputDirectorySelector, false);
    }
}

