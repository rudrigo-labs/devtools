using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DevTools.Host.Wpf.Facades;
using DevTools.Host.Wpf.Services;
using DevTools.Snapshot.Models;

namespace DevTools.Host.Wpf.Views;

public partial class SnapshotWorkspaceView : System.Windows.Controls.UserControl
{
    private const string ToolHistorySlug = "snapshot";
    private const string ToolDisplayName = "Snapshot";
    private const string NoConfigurationOptionLabel = "Configurar manualmente";

    private enum SnapshotWorkspaceMode
    {
        Execution,
        Configuration
    }

    private readonly ObservableCollection<SnapshotEntity> _entities = new();
    private readonly ObservableCollection<SnapshotSelectionOption> _configurationOptions = new();
    private readonly ISnapshotFacade _facade;
    private SnapshotEntity? _currentEntity;
    private SnapshotWorkspaceMode _currentMode = SnapshotWorkspaceMode.Execution;
    private CancellationTokenSource? _executionCts;
    private bool _isExecuting;
    private bool _initialized;
    private bool _suppressConfigurationSelectionChanged;
    private bool _isConfigurationDraft;

    public SnapshotWorkspaceView(ISnapshotFacade facade)
    {
        _facade = facade;
        InitializeComponent();
        ConfigurationsCombo.ItemsSource = _configurationOptions;
        Loaded += SnapshotWorkspaceView_Loaded;
        ApplyModeState();
    }

    private async void SnapshotWorkspaceView_Loaded(object sender, RoutedEventArgs e)
    {
        if (_initialized)
            return;

        _initialized = true;
        await ReloadEntitiesAsync().ConfigureAwait(true);
    }

    public void ActivateExecutionMode()
    {
        if (_isExecuting)
            return;

        if (_currentEntity is null)
            _currentEntity = CreateUnboundExecutionEntity();

        BindEntityToForm(_currentEntity);
        _isConfigurationDraft = false;
        SetMode(SnapshotWorkspaceMode.Execution, "Modo execução ativado.");
    }

    public void ActivateConfigurationMode()
    {
        if (_isExecuting)
            return;

        SetMode(SnapshotWorkspaceMode.Configuration, "Modo configuração ativado.");
        ResetConfigurationState();
    }

    private async Task ReloadEntitiesAsync()
    {
        var selectedId = _currentEntity?.Id;
        var list = await _facade.LoadAsync();
        _entities.Clear();
        foreach (var item in list)
        {
            ApplyFixedDefaults(item);
            _entities.Add(item);
        }

        _configurationOptions.Clear();
        if (_entities.Count > 0)
        {
            _configurationOptions.Add(new SnapshotSelectionOption(NoConfigurationOptionLabel, null));
            foreach (var item in _entities)
            {
                _configurationOptions.Add(new SnapshotSelectionOption(item.Name, item));
            }
        }

        if (_entities.Count == 0)
        {
            SetSelectedConfigurationOption(null);
            CreateNewEntity();
            return;
        }

        SnapshotEntity? selected = null;
        if (!string.IsNullOrWhiteSpace(selectedId))
        {
            selected = _entities.FirstOrDefault(x => x.Id == selectedId)
                ?? _entities.FirstOrDefault(x => x.IsDefault)
                ?? _entities.FirstOrDefault();
        }

        var selectedOption = selected is null
            ? GetNoConfigurationOption()
            : _configurationOptions.FirstOrDefault(x => x.Entity?.Id == selected.Id) ?? GetNoConfigurationOption();

        SetSelectedConfigurationOption(selectedOption);
        ApplySelectedConfigurationOption(selectedOption);
    }

    private void ConfigurationsCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressConfigurationSelectionChanged)
            return;

        if (ConfigurationsCombo.SelectedItem is not SnapshotSelectionOption option)
            return;

        ApplySelectedConfigurationOption(option);
        if (_currentMode == SnapshotWorkspaceMode.Configuration)
        {
            _isConfigurationDraft = option.Entity is not null;
            ApplyModeState();
        }
    }

    private async void ActionSave_Click(object sender, RoutedEventArgs e)
    {
        if (_isExecuting)
            return;

        if (_currentMode == SnapshotWorkspaceMode.Execution)
        {
            await ExecuteCurrentAsync().ConfigureAwait(true);
            return;
        }

        if (!_isConfigurationDraft)
        {
            ValidationUiService.ShowInline(ExecutionStatusText, "Clique em Novo para iniciar uma configuração.");
            return;
        }

        if (!TryBindFormToEntity(out var entity, out var validationError, requireName: true))
        {
            ValidationUiService.ShowInline(ExecutionStatusText, validationError);
            return;
        }

        var result = await _facade.SaveAsync(entity);
        if (!result.IsValid)
        {
            ValidationUiService.ShowInline(ExecutionStatusText, JoinValidationErrors(result));
            return;
        }

        ValidationUiService.ClearInline(ExecutionStatusText);
        _currentEntity = entity;
        await ReloadEntitiesAsync().ConfigureAwait(true);
        ExecutionStatusText.Text = $"Configuração '{entity.Name}' salva.";
        ResetConfigurationState();
    }

    private async void ActionDelete_Click(object sender, RoutedEventArgs e)
    {
        if (_isExecuting || _currentMode != SnapshotWorkspaceMode.Configuration)
            return;

        if (_currentEntity is null)
            return;

        if (string.IsNullOrWhiteSpace(_currentEntity.Id))
        {
            CreateNewEntity();
            return;
        }

        await _facade.DeleteAsync(_currentEntity.Id);
        ValidationUiService.ClearInline(ExecutionStatusText);
        await ReloadEntitiesAsync().ConfigureAwait(true);
        ExecutionStatusText.Text = "Configuração removida.";
        SetMode(SnapshotWorkspaceMode.Execution);
    }

    private void ActionNew_Click(object sender, RoutedEventArgs e)
    {
        if (_isExecuting)
            return;

        if (_currentMode == SnapshotWorkspaceMode.Execution)
        {
            SetMode(SnapshotWorkspaceMode.Configuration, "Modo configuração ativado.");
            ResetConfigurationState();
            return;
        }

        _isConfigurationDraft = true;
        CreateNewEntity();
        ValidationUiService.ClearInline(ExecutionStatusText);
        ExecutionStatusText.Text = "Nova configuração criada (não salva).";
        ApplyModeState();
    }

    private void ActionCancel_Click(object sender, RoutedEventArgs e)
    {
        if (_currentMode == SnapshotWorkspaceMode.Configuration)
        {
            ResetConfigurationState();
            ValidationUiService.ClearInline(ExecutionStatusText);
            ExecutionStatusText.Text = "Configuração cancelada.";
            return;
        }

        ActionBack_Click(sender, e);
    }

    private void ActionGoToTool_Click(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is MainWindow mainWindow)
        {
            mainWindow.OpenToolExecution("Snapshot");
            return;
        }

        SetMode(SnapshotWorkspaceMode.Execution, "Modo execução ativado.");
    }

    private void ActionBack_Click(object sender, RoutedEventArgs e)
    {
        if (_isExecuting)
        {
            _executionCts?.Cancel();
            ExecutionStatusText.Text = "Cancelando execução...";
            return;
        }

        if (Window.GetWindow(this) is MainWindow mainWindow)
        {
            mainWindow.OpenFerramentasHome();
        }
    }

    private async void ExecuteButton_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteCurrentAsync().ConfigureAwait(true);
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        ActionCancel_Click(sender, e);
    }

    private async void HistoryButton_Click(object sender, RoutedEventArgs e)
        => await ToolHistoryViewHelper.ShowAndApplyAsync(WorkspaceRoot, ToolHistorySlug, ToolDisplayName, ExecutionStatusText).ConfigureAwait(true);

    private async Task ExecuteCurrentAsync()
    {
        if (_isExecuting)
            return;

        if (!TryBindFormToEntity(out var entity, out var validationError, requireName: false))
        {
            ValidationUiService.ShowInline(ExecutionStatusText, validationError);
            return;
        }

        if (ValidateDotNetCheck.IsChecked == true && !DotNetProjectValidator.HasDotNetProject(entity.RootPath))
        {
            ValidationUiService.ShowInline(ExecutionStatusText,
                "Nenhum projeto .NET encontrado na pasta raiz (.csproj, .sln ou .slnx).");
            return;
        }

        var request = new SnapshotRequest
        {
            RootPath = entity.RootPath,
            OutputBasePath = entity.OutputBasePath,
            GenerateText = entity.GenerateText,
            GenerateHtmlPreview = entity.GenerateHtmlPreview,
            GenerateJsonNested = entity.GenerateJsonNested,
            GenerateJsonRecursive = entity.GenerateJsonRecursive,
            IgnoredDirectories = entity.IgnoredDirectories,
            IgnoredExtensions = entity.IgnoredExtensions,
            IncludedExtensions = entity.IncludedExtensions,
            MaxFileSizeKb = entity.MaxFileSizeKb
        };

        await ToolHistoryViewHelper.RecordAsync(ToolHistorySlug, WorkspaceRoot, "Executar snapshot").ConfigureAwait(true);

        _executionCts?.Dispose();
        _executionCts = new CancellationTokenSource();
        _isExecuting = true;
        ApplyModeState();
        ExecutionStatusText.Text = "Executando snapshot...";

        try
        {
            var result = await _facade.ExecuteAsync(request, _executionCts.Token).ConfigureAwait(true);
            if (!result.IsSuccess)
            {
                ValidationUiService.ShowInline(ExecutionStatusText, string.Join(" | ", result.Errors.Select(x => x.Message)));
                ExecutionStatusText.Text = "Falha na execução do snapshot.";
                return;
            }

            ValidationUiService.ClearInline(ExecutionStatusText);
            var data = result.Value!;
            ExecutionStatusText.Text = $"Snapshot executado. Arquivos analisados: {data.TotalFilesScanned}.";
        }
        catch (OperationCanceledException)
        {
            ValidationUiService.ClearInline(ExecutionStatusText);
            ExecutionStatusText.Text = "Execução cancelada.";
        }
        finally
        {
            _isExecuting = false;
            _executionCts?.Dispose();
            _executionCts = null;
            ApplyModeState();
        }
    }

    private void CreateNewEntity()
    {
        _currentEntity = new SnapshotEntity
        {
            Name = "Snapshot 1",
            Description = string.Empty,
            IsActive = true,
            GenerateText = true,
            IgnoredDirectories = SnapshotDefaults.DefaultIgnoredDirectories,
            IgnoredExtensions = SnapshotDefaults.DefaultIgnoredExtensions,
            IncludedExtensions = SnapshotDefaults.DefaultIncludedExtensions,
            MaxFileSizeKb = null
        };

        SetSelectedConfigurationOption(GetNoConfigurationOption());
        BindEntityToForm(_currentEntity);
        ApplyModeState();
    }

    private void BindEntityToForm(SnapshotEntity entity)
    {
        ApplyFixedDefaults(entity);
        NameInput.Text = entity.Name;
        DescriptionInput.Text = entity.Description;
        RootPathSelector.SelectedPath = entity.RootPath;
        OutputBasePathSelector.SelectedPath = entity.OutputBasePath;
        IgnoredDirectoriesInput.Text = string.Join(", ", entity.IgnoredDirectories);
        IgnoredExtensionsInput.Text = string.Join(", ", entity.IgnoredExtensions);
        IncludedExtensionsInput.Text = entity.IncludedExtensions.Count > 0
            ? string.Join(", ", entity.IncludedExtensions)
            : string.Empty;
        TextCheck.IsChecked = entity.GenerateText;
        HtmlCheck.IsChecked = entity.GenerateHtmlPreview;
        JsonNestedCheck.IsChecked = entity.GenerateJsonNested;
        JsonRecursiveCheck.IsChecked = entity.GenerateJsonRecursive;
    }

    private bool TryBindFormToEntity(out SnapshotEntity entity, out string errorMessage, bool requireName)
    {
        entity = _currentEntity is null ? new SnapshotEntity() : CloneEntity(_currentEntity);

        ClearInlineValidationStates();

        if (!requireName &&
            !ValidationUiService.ValidateRequiredFields(
                out errorMessage,
                ValidationUiService.RequiredPath("Pasta do projeto", RootPathSelector, RootPathSelector.SelectedPath),
                ValidationUiService.RequiredPath("Pasta de saída", OutputBasePathSelector, OutputBasePathSelector.SelectedPath),
                ValidationUiService.RequiredControl("Diretórios ignorados", IgnoredDirectoriesInput, IgnoredDirectoriesInput.Text)))
        {
            return false;
        }

        if (requireName &&
            !ValidationUiService.ValidateRequiredFields(
                out errorMessage,
                ValidationUiService.RequiredControl("Nome", NameInput, NameInput.Text),
                ValidationUiService.RequiredControl("Descrição", DescriptionInput, DescriptionInput.Text),
                ValidationUiService.RequiredPath("Pasta do projeto", RootPathSelector, RootPathSelector.SelectedPath),
                ValidationUiService.RequiredPath("Pasta de saída", OutputBasePathSelector, OutputBasePathSelector.SelectedPath),
                ValidationUiService.RequiredControl("Diretórios ignorados", IgnoredDirectoriesInput, IgnoredDirectoriesInput.Text)))
        {
            return false;
        }

        if (!ValidateOutputSelection(out errorMessage))
        {
            return false;
        }

        var name = NameInput.Text.Trim();
        entity.Name = requireName
            ? name
            : (string.IsNullOrWhiteSpace(name) ? entity.Name : name);
        entity.Description = DescriptionInput.Text.Trim();
        entity.RootPath = RootPathSelector.SelectedPath?.Trim() ?? string.Empty;
        entity.OutputBasePath = OutputBasePathSelector.SelectedPath?.Trim() ?? string.Empty;
        entity.IgnoredDirectories = ParseList(IgnoredDirectoriesInput.Text, SnapshotDefaults.DefaultIgnoredDirectories);
        entity.IgnoredExtensions = ParseList(IgnoredExtensionsInput.Text, SnapshotDefaults.DefaultIgnoredExtensions);
        entity.IncludedExtensions = ParseList(IncludedExtensionsInput.Text, Array.Empty<string>());
        entity.MaxFileSizeKb = null;
        entity.GenerateText = TextCheck.IsChecked ?? false;
        entity.GenerateHtmlPreview = HtmlCheck.IsChecked ?? false;
        entity.GenerateJsonNested = JsonNestedCheck.IsChecked ?? false;
        entity.GenerateJsonRecursive = JsonRecursiveCheck.IsChecked ?? false;
        entity.IsActive = true;

        _currentEntity = entity;
        errorMessage = string.Empty;
        return true;
    }

    private void SetMode(SnapshotWorkspaceMode mode, string? statusMessage = null)
    {
        _currentMode = mode;
        ApplyModeState();

        if (!string.IsNullOrWhiteSpace(statusMessage))
        {
            ValidationUiService.ClearInline(ExecutionStatusText);
            ExecutionStatusText.Text = statusMessage;
        }
    }

    private void ApplyModeState()
    {
        var hasSelected = _currentEntity is not null;
        var hasConfigurations = _entities.Count > 0;
        var inConfiguration = _currentMode == SnapshotWorkspaceMode.Configuration;
        var inExecution = _currentMode == SnapshotWorkspaceMode.Execution;

        ConfigurationsLabel.Visibility = hasConfigurations ? Visibility.Visible : Visibility.Collapsed;
        ConfigurationsCombo.Visibility = hasConfigurations ? Visibility.Visible : Visibility.Collapsed;
        ConfigurationMetadataSection.Visibility = inConfiguration ? Visibility.Visible : Visibility.Collapsed;
        ConfigurationModeHint.Visibility = Visibility.Collapsed;

        WorkspaceTitleText.Text = inConfiguration ? "Snapshot - Configuração" : "Snapshot";
        WorkspaceSubtitleText.Text = inConfiguration
            ? "Defina os campos de configuração abaixo e salve para reutilizar."
            : "Gera um snapshot textual completo do projeto para uso com assistentes de IA.";

        Actions.NewText = "Novo";
        Actions.SaveText = inConfiguration ? "Salvar" : "Executar";
        Actions.SaveIconKind = inConfiguration ? "ContentSave" : "Play";
        Actions.CancelText = "Cancelar";
        Actions.GoToToolText = "Ir para ferramenta";
        Actions.BackText = _isExecuting ? "Cancelar execução" : "Voltar";
        Actions.BackIconKind = _isExecuting ? "CloseCircleOutline" : "ArrowLeft";

        Actions.ShowHelp = true;
        Actions.ShowHistory = inExecution;
        Actions.HelpContextKey = inConfiguration ? "snapshot:configuration" : "snapshot:execution";
        Actions.ShowNew = inConfiguration;
        Actions.ShowSave = inConfiguration || inExecution;
        Actions.ShowDelete = false;
        Actions.ShowCancel = inConfiguration;
        Actions.ShowGoToTool = false;
        Actions.ShowBack = inExecution;

        Actions.CanHelp = true;
        Actions.CanNew = inConfiguration && !_isExecuting && !_isConfigurationDraft;
        Actions.CanSave = !_isExecuting && (inExecution ? hasSelected : _isConfigurationDraft);
        Actions.CanDelete = false;
        Actions.CanCancel = inConfiguration && !_isExecuting && _isConfigurationDraft;
        Actions.CanGoToTool = false;
        Actions.CanBack = inExecution;
    }

    private static SnapshotEntity CloneEntity(SnapshotEntity source)
    {
        return new SnapshotEntity
        {
            Id = source.Id,
            Name = source.Name,
            Description = source.Description,
            IsActive = source.IsActive,
            IsDefault = source.IsDefault,
            CreatedAtUtc = source.CreatedAtUtc,
            UpdatedAtUtc = source.UpdatedAtUtc,
            RootPath = source.RootPath,
            OutputBasePath = source.OutputBasePath,
            GenerateText = source.GenerateText,
            GenerateHtmlPreview = source.GenerateHtmlPreview,
            GenerateJsonNested = source.GenerateJsonNested,
            GenerateJsonRecursive = source.GenerateJsonRecursive,
            IgnoredDirectories = source.IgnoredDirectories.ToArray(),
            IgnoredExtensions = source.IgnoredExtensions.ToArray(),
            IncludedExtensions = source.IncludedExtensions.ToArray(),
            MaxFileSizeKb = source.MaxFileSizeKb
        };
    }

    private static IReadOnlyList<string> ParseList(string? input, IReadOnlyList<string> fallback)
    {
        if (string.IsNullOrWhiteSpace(input))
            return fallback;

        var items = input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                         .Where(x => !string.IsNullOrWhiteSpace(x))
                         .ToArray();

        return items.Length > 0 ? items : fallback;
    }


    private static void ApplyFixedDefaults(SnapshotEntity entity)
    {
        if (entity.IgnoredDirectories is null || entity.IgnoredDirectories.Count == 0)
            entity.IgnoredDirectories = SnapshotDefaults.DefaultIgnoredDirectories;

        if (entity.IgnoredExtensions is null || entity.IgnoredExtensions.Count == 0)
            entity.IgnoredExtensions = SnapshotDefaults.DefaultIgnoredExtensions;

        if (entity.IncludedExtensions is null || entity.IncludedExtensions.Count == 0)
            entity.IncludedExtensions = SnapshotDefaults.DefaultIncludedExtensions;
    }

    private static string JoinValidationErrors(DevTools.Core.Validation.ValidationResult validationResult)
    {
        if (validationResult.IsValid)
            return string.Empty;

        return string.Join(" | ", validationResult.Errors.Select(x => x.Message));
    }

    private void ClearInlineValidationStates()
    {
        ValidationUiService.SetControlInvalid(NameInput, false);
        ValidationUiService.SetControlInvalid(DescriptionInput, false);
        ValidationUiService.SetPathSelectorInvalid(RootPathSelector, false);
        ValidationUiService.SetPathSelectorInvalid(OutputBasePathSelector, false);
        ValidationUiService.SetControlInvalid(IgnoredDirectoriesInput, false);
        ValidationUiService.SetControlInvalid(IgnoredExtensionsInput, false);
        ValidationUiService.SetControlInvalid(IncludedExtensionsInput, false);
        SetOutputSelectionInvalid(false);
    }

    private bool ValidateOutputSelection(out string errorMessage)
    {
        var hasAnyOutput =
            (TextCheck.IsChecked ?? false) ||
            (HtmlCheck.IsChecked ?? false) ||
            (JsonNestedCheck.IsChecked ?? false) ||
            (JsonRecursiveCheck.IsChecked ?? false);

        SetOutputSelectionInvalid(!hasAnyOutput);
        if (!hasAnyOutput)
        {
            errorMessage = "Selecione pelo menos um formato de saída.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    private void SetOutputSelectionInvalid(bool invalid)
    {
        ValidationUiService.SetControlInvalid(TextCheck, invalid);
        ValidationUiService.SetControlInvalid(HtmlCheck, invalid);
        ValidationUiService.SetControlInvalid(JsonNestedCheck, invalid);
        ValidationUiService.SetControlInvalid(JsonRecursiveCheck, invalid);
    }

    private void ApplySelectedConfigurationOption(SnapshotSelectionOption? option)
    {
        if (option?.Entity is null)
        {
            _currentEntity = CreateUnboundExecutionEntity();
        }
        else
        {
            _currentEntity = option.Entity;
        }

        BindEntityToForm(_currentEntity);
        ValidationUiService.ClearInline(ExecutionStatusText);
        if (_currentMode == SnapshotWorkspaceMode.Configuration)
            _isConfigurationDraft = option?.Entity is not null;
        ApplyModeState();
    }

    private void ResetConfigurationState()
    {
        _isConfigurationDraft = false;
        _currentEntity = CreateUnboundExecutionEntity();
        SetSelectedConfigurationOption(GetNoConfigurationOption());
        BindEntityToForm(_currentEntity);
        ValidationUiService.ClearInline(ExecutionStatusText);
        ApplyModeState();
    }

    private SnapshotSelectionOption? GetNoConfigurationOption() =>
        _configurationOptions.FirstOrDefault(x => x.Entity is null);

    private void SetSelectedConfigurationOption(SnapshotSelectionOption? option)
    {
        _suppressConfigurationSelectionChanged = true;
        try
        {
            ConfigurationsCombo.SelectedItem = option;
        }
        finally
        {
            _suppressConfigurationSelectionChanged = false;
        }
    }

    private static SnapshotEntity CreateUnboundExecutionEntity()
    {
        return new SnapshotEntity
        {
            IsActive = true,
            GenerateText = true,
            IgnoredDirectories = SnapshotDefaults.DefaultIgnoredDirectories,
            IgnoredExtensions = SnapshotDefaults.DefaultIgnoredExtensions,
            IncludedExtensions = SnapshotDefaults.DefaultIncludedExtensions,
            MaxFileSizeKb = null
        };
    }

    private sealed class SnapshotSelectionOption
    {
        public SnapshotSelectionOption(string name, SnapshotEntity? entity)
        {
            Name = name;
            Entity = entity;
        }

        public string Name { get; }
        public SnapshotEntity? Entity { get; }
    }
}

