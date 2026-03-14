using System.Windows;
using System.Windows.Controls;
using DevTools.Host.Wpf.Facades;
using DevTools.Host.Wpf.Services;
using DevTools.Rename.Models;

namespace DevTools.Host.Wpf.Views;

public partial class RenameWorkspaceView : System.Windows.Controls.UserControl
{
    private readonly IRenameFacade _facade;
    private CancellationTokenSource? _executionCts;
    private bool _isExecuting;

    public RenameWorkspaceView(IRenameFacade facade)
    {
        _facade = facade;
        InitializeComponent();
        ApplyDefaults();
        ApplyModeState();
    }

    private void ApplyDefaults()
    {
        IgnoredDirectoriesInput.Text = string.Join(", ", RenameDefaults.DefaultIgnoredDirectories);
        IgnoredExtensionsInput.Text  = string.Join(", ", RenameDefaults.DefaultIgnoredExtensions);
        IncludedExtensionsInput.Text = string.Join(", ", RenameDefaults.DefaultIncludedExtensions);
    }

    // -------------------------------------------------------------------------
    // Action bar
    // -------------------------------------------------------------------------

    private async void ActionSave_Click(object sender, RoutedEventArgs e)
        => await ExecuteAsync().ConfigureAwait(true);

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

    // -------------------------------------------------------------------------
    // Execução
    // -------------------------------------------------------------------------

    private async Task ExecuteAsync()
    {
        if (_isExecuting)
            return;

        if (!TryBuildRequest(out var request, out var errorMessage))
        {
            ValidationUiService.ShowInline(ExecutionStatusText, errorMessage);
            return;
        }

        if (ValidateDotNetCheck.IsChecked == true && !DotNetProjectValidator.HasDotNetProject(request.RootPath))
        {
            ValidationUiService.ShowInline(ExecutionStatusText,
                "Nenhum projeto .NET encontrado na pasta raiz (.csproj, .sln ou .slnx).");
            return;
        }

        _executionCts?.Dispose();
        _executionCts = new CancellationTokenSource();
        _isExecuting = true;
        ApplyModeState();
        ExecutionStatusText.Text = request.DryRun ? "Simulando..." : "Executando...";

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
            var s = result.Value!.Summary;

            ExecutionStatusText.Text = request.DryRun
                ? $"Simulacao concluida — arquivos: {s.FilesScanned} | alteracoes previstas: {s.FilesUpdated + s.FilesRenamed} | diretorios: {s.DirectoriesRenamed}"
                : $"Concluido — arquivos alterados: {s.FilesUpdated} | renomeados: {s.FilesRenamed} | diretorios: {s.DirectoriesRenamed} | erros: {s.Errors}";
        }
        catch (OperationCanceledException)
        {
            ValidationUiService.ClearInline(ExecutionStatusText);
            ExecutionStatusText.Text = "Execucao cancelada.";
        }
        finally
        {
            _isExecuting = false;
            _executionCts?.Dispose();
            _executionCts = null;
            ApplyModeState();
        }
    }

    // -------------------------------------------------------------------------
    // Build request
    // -------------------------------------------------------------------------

    private bool TryBuildRequest(out RenameRequest request, out string errorMessage)
    {
        request = null!;
        ClearValidationStates();

        if (!ValidationUiService.ValidateRequiredFields(
                out errorMessage,
                ValidationUiService.RequiredPath("Pasta raiz", RootPathSelector, RootPathSelector.SelectedPath),
                ValidationUiService.RequiredControl("Texto antigo", OldTextInput, OldTextInput.Text),
                ValidationUiService.RequiredControl("Texto novo", NewTextInput, NewTextInput.Text)))
        {
            return false;
        }

        request = new RenameRequest
        {
            RootPath           = RootPathSelector.SelectedPath!.Trim(),
            OldText            = OldTextInput.Text.Trim(),
            NewText            = NewTextInput.Text.Trim(),
            Mode               = ModeNamespaceRadio.IsChecked == true ? RenameMode.NamespaceOnly : RenameMode.General,
            DryRun             = DryRunCheck.IsChecked ?? false,
            BackupEnabled      = true,
            WriteUndoLog       = true,
            MaxDiffLinesPerFile = 200,
            IgnoredDirectories = ParseList(IgnoredDirectoriesInput.Text, RenameDefaults.DefaultIgnoredDirectories),
            IgnoredExtensions  = ParseList(IgnoredExtensionsInput.Text, RenameDefaults.DefaultIgnoredExtensions),
            IncludedExtensions = ParseList(IncludedExtensionsInput.Text, RenameDefaults.DefaultIncludedExtensions),
        };

        errorMessage = string.Empty;
        return true;
    }

    // -------------------------------------------------------------------------
    // Estado visual
    // -------------------------------------------------------------------------

    private void ApplyModeState()
    {
        Actions.ShowHelp = true;
        Actions.NewText = "Novo";
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

    private void ClearValidationStates()
    {
        ValidationUiService.SetPathSelectorInvalid(RootPathSelector, false);
        ValidationUiService.SetControlInvalid(OldTextInput, false);
        ValidationUiService.SetControlInvalid(NewTextInput, false);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static IReadOnlyList<string> ParseList(string? input, IReadOnlyList<string> fallback)
    {
        if (string.IsNullOrWhiteSpace(input))
            return fallback;

        var items = input
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();

        return items.Length > 0 ? items : fallback;
    }
}
