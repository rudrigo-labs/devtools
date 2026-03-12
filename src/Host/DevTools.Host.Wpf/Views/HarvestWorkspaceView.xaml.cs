using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using DevTools.Harvest.Models;
using DevTools.Host.Wpf.Facades;
using DevTools.Host.Wpf.Services;

namespace DevTools.Host.Wpf.Views;

public partial class HarvestWorkspaceView : System.Windows.Controls.UserControl
{
    private const string NoConfigurationOptionLabel = "Configurar manualmente";

    private enum HarvestWorkspaceMode
    {
        Execution,
        Configuration
    }

    private readonly ObservableCollection<HarvestEntity> _entities = new();
    private readonly ObservableCollection<HarvestSelectionOption> _configurationOptions = new();
    private readonly IHarvestFacade _facade;
    private HarvestEntity? _currentEntity;
    private HarvestWorkspaceMode _currentMode = HarvestWorkspaceMode.Execution;
    private CancellationTokenSource? _executionCts;
    private bool _isExecuting;
    private bool _initialized;
    private bool _suppressConfigurationSelectionChanged;

    public HarvestWorkspaceView(IHarvestFacade facade)
    {
        _facade = facade;
        InitializeComponent();
        ConfigurationsCombo.ItemsSource = _configurationOptions;
        Loaded += HarvestWorkspaceView_Loaded;
        ApplyModeState();
    }

    private async void HarvestWorkspaceView_Loaded(object sender, RoutedEventArgs e)
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
        SetMode(HarvestWorkspaceMode.Execution, "Modo execução ativado.");
    }

    public void ActivateConfigurationMode()
    {
        if (_isExecuting)
            return;

        if (_currentEntity is null)
            _currentEntity = CreateUnboundExecutionEntity();

        BindEntityToForm(_currentEntity);
        SetMode(HarvestWorkspaceMode.Configuration, "Modo configuração ativado.");
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
            _configurationOptions.Add(new HarvestSelectionOption(NoConfigurationOptionLabel, null));
            foreach (var item in _entities)
                _configurationOptions.Add(new HarvestSelectionOption(item.Name, item));
        }

        if (_entities.Count == 0)
        {
            SetSelectedConfigurationOption(null);
            CreateNewEntity();
            return;
        }

        HarvestEntity? selected = null;
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

        if (ConfigurationsCombo.SelectedItem is not HarvestSelectionOption option)
            return;

        ApplySelectedConfigurationOption(option);
    }

    private async void ActionSave_Click(object sender, RoutedEventArgs e)
    {
        if (_isExecuting)
            return;

        if (_currentMode == HarvestWorkspaceMode.Execution)
        {
            await ExecuteCurrentAsync().ConfigureAwait(true);
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
        SetMode(HarvestWorkspaceMode.Execution);
    }

    private async void ActionDelete_Click(object sender, RoutedEventArgs e)
    {
        if (_isExecuting || _currentMode != HarvestWorkspaceMode.Configuration)
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
        SetMode(HarvestWorkspaceMode.Execution);
    }

    private void ActionNew_Click(object sender, RoutedEventArgs e)
    {
        if (_isExecuting)
            return;

        if (_currentMode == HarvestWorkspaceMode.Execution)
        {
            SetMode(HarvestWorkspaceMode.Configuration, "Modo configuração ativado.");
            if (_currentEntity is not null)
                BindEntityToForm(_currentEntity);
            return;
        }

        CreateNewEntity();
        ValidationUiService.ClearInline(ExecutionStatusText);
        ExecutionStatusText.Text = "Nova configuração criada (não salva).";
        ApplyModeState();
    }

    private void ActionCancel_Click(object sender, RoutedEventArgs e)
    {
        if (_isExecuting)
        {
            _executionCts?.Cancel();
            ExecutionStatusText.Text = "Cancelando execução...";
            return;
        }

        if (_currentMode == HarvestWorkspaceMode.Configuration)
        {
            if (_currentEntity is not null)
                BindEntityToForm(_currentEntity);

            SetMode(HarvestWorkspaceMode.Execution, "Modo execução ativado.");
            return;
        }

        if (_currentEntity is not null)
            BindEntityToForm(_currentEntity);

        ValidationUiService.ClearInline(ExecutionStatusText);
        ExecutionStatusText.Text = "Execução pronta.";
        ApplyModeState();
    }

    // ── Execução ─────────────────────────────────────────────────────────────

    private async Task ExecuteCurrentAsync()
    {
        if (_isExecuting)
            return;

        if (!TryBindFormToEntity(out var entity, out var validationError, requireName: false))
        {
            ValidationUiService.ShowInline(ExecutionStatusText, validationError);
            return;
        }

        var request = new HarvestRequest
        {
            RootPath = entity.RootPath,
            OutputPath = entity.OutputPath,
            CopyFiles = entity.CopyFiles,
            MinScore = entity.MinScore,
            IgnoredDirectories = entity.IgnoredDirectories,
            IgnoredExtensions = entity.IgnoredExtensions,
            IncludedExtensions = entity.IncludedExtensions,
            MaxFileSizeKb = entity.MaxFileSizeKb,
            FanInWeight = entity.FanInWeight,
            FanOutWeight = entity.FanOutWeight,
            KeywordDensityWeight = entity.KeywordDensityWeight,
            DensityScale = entity.DensityScale,
            StaticMethodThreshold = entity.StaticMethodThreshold,
            StaticMethodBonus = entity.StaticMethodBonus,
            DeadCodePenalty = entity.DeadCodePenalty,
            LargeFileThresholdLines = entity.LargeFileThresholdLines,
            LargeFilePenalty = entity.LargeFilePenalty,
            Categories = entity.Categories
        };

        _executionCts?.Dispose();
        _executionCts = new CancellationTokenSource();
        _isExecuting = true;
        ApplyModeState();
        ExecutionStatusText.Text = "Executando harvest...";

        try
        {
            var result = await _facade.ExecuteAsync(request, _executionCts.Token).ConfigureAwait(true);
            if (!result.IsSuccess)
            {
                ValidationUiService.ShowInline(ExecutionStatusText, string.Join(" | ", result.Errors.Select(x => x.Message)));
                return;
            }

            ValidationUiService.ClearInline(ExecutionStatusText);
            var data = result.Value!.Report;
            var copiedMsg = entity.CopyFiles ? $" Copiados: {data.TotalFilesScored}." : " (dry run)";
            ExecutionStatusText.Text = $"Harvest concluído. Analisados: {data.TotalFilesAnalyzed}. Selecionados: {data.TotalFilesScored}.{copiedMsg}";
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

    // ── Criação e vínculo de entidade ─────────────────────────────────────────

    private void CreateNewEntity()
    {
        var nextIndex = _entities.Count + 1;
        _currentEntity = new HarvestEntity
        {
            Name = $"Harvest {nextIndex}",
            Description = "Nova configuração",
            IsActive = true,
            CopyFiles = true,
            IgnoredDirectories = HarvestDefaults.DefaultIgnoredDirectories,
            IgnoredExtensions = HarvestDefaults.DefaultIgnoredExtensions,
            IncludedExtensions = HarvestDefaults.DefaultIncludedExtensions,
        };

        SetSelectedConfigurationOption(GetNoConfigurationOption());
        BindEntityToForm(_currentEntity);
        ApplyModeState();
    }

    private void BindEntityToForm(HarvestEntity entity)
    {
        ApplyFixedDefaults(entity);
        NameInput.Text = entity.Name;
        DescriptionInput.Text = entity.Description;
        RootPathSelector.SelectedPath = entity.RootPath;
        OutputPathSelector.SelectedPath = entity.OutputPath;
        IgnoredDirectoriesInput.Text = string.Join(", ", entity.IgnoredDirectories);
        IgnoredExtensionsInput.Text = string.Join(", ", entity.IgnoredExtensions);
        IncludedExtensionsInput.Text = entity.IncludedExtensions.Count > 0
            ? string.Join(", ", entity.IncludedExtensions)
            : string.Empty;
        MinScoreInput.Text = entity.MinScore.ToString();
        CopyFilesCheck.IsChecked = entity.CopyFiles;
    }

    private bool TryBindFormToEntity(out HarvestEntity entity, out string errorMessage, bool requireName)
    {
        entity = _currentEntity is null ? new HarvestEntity() : CloneEntity(_currentEntity);

        ClearInlineValidationStates();

        var baseFields = new[]
        {
            ValidationUiService.RequiredPath("Pasta de origem", RootPathSelector, RootPathSelector.SelectedPath),
            ValidationUiService.RequiredPath("Pasta de destino", OutputPathSelector, OutputPathSelector.SelectedPath),
            ValidationUiService.RequiredControl("Diretórios ignorados", IgnoredDirectoriesInput, IgnoredDirectoriesInput.Text)
        };

        if (requireName)
        {
            var configFields = new[]
            {
                ValidationUiService.RequiredControl("Nome", NameInput, NameInput.Text),
                ValidationUiService.RequiredControl("Descrição", DescriptionInput, DescriptionInput.Text)
            };

            if (!ValidationUiService.ValidateRequiredFields(out errorMessage, [.. configFields, .. baseFields]))
                return false;
        }
        else
        {
            if (!ValidationUiService.ValidateRequiredFields(out errorMessage, baseFields))
                return false;
        }

        var name = NameInput.Text.Trim();
        entity.Name = requireName
            ? name
            : (string.IsNullOrWhiteSpace(name) ? entity.Name : name);
        entity.Description = DescriptionInput.Text.Trim();
        entity.RootPath = RootPathSelector.SelectedPath?.Trim() ?? string.Empty;
        entity.OutputPath = OutputPathSelector.SelectedPath?.Trim() ?? string.Empty;
        entity.IgnoredDirectories = ParseList(IgnoredDirectoriesInput.Text, HarvestDefaults.DefaultIgnoredDirectories);
        entity.IgnoredExtensions = ParseList(IgnoredExtensionsInput.Text, HarvestDefaults.DefaultIgnoredExtensions);
        entity.IncludedExtensions = ParseList(IncludedExtensionsInput.Text, HarvestDefaults.DefaultIncludedExtensions);
        entity.MinScore = int.TryParse(MinScoreInput.Text.Trim(), out var ms) ? Math.Max(0, ms) : 0;
        entity.CopyFiles = CopyFilesCheck.IsChecked ?? true;
        entity.IsActive = true;
        entity.MaxFileSizeKb = null;

        _currentEntity = entity;
        errorMessage = string.Empty;
        return true;
    }

    // ── Modo ─────────────────────────────────────────────────────────────────

    private void SetMode(HarvestWorkspaceMode mode, string? statusMessage = null)
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
        var hasPersisted = hasSelected && !string.IsNullOrWhiteSpace(_currentEntity!.Id) && _entities.Any(x => x.Id == _currentEntity.Id);
        var hasConfigurations = _entities.Count > 0;
        var inConfiguration = _currentMode == HarvestWorkspaceMode.Configuration;

        ConfigurationsLabel.Visibility = hasConfigurations ? Visibility.Visible : Visibility.Collapsed;
        ConfigurationsCombo.Visibility = hasConfigurations ? Visibility.Visible : Visibility.Collapsed;
        ConfigurationMetadataSection.Visibility = inConfiguration ? Visibility.Visible : Visibility.Collapsed;

        Actions.NewText = inConfiguration ? "Novo" : "Configurar";
        Actions.SaveText = inConfiguration ? "Salvar" : "Executar";
        Actions.CancelText = _isExecuting ? "Cancelar Execução" : "Cancelar";

        Actions.ShowNew = !_isExecuting;
        Actions.ShowSave = !_isExecuting;
        Actions.ShowDelete = inConfiguration && !_isExecuting;
        Actions.ShowCancel = _isExecuting || hasSelected;
        Actions.CanNew = !_isExecuting;
        Actions.CanSave = hasSelected && !_isExecuting;
        Actions.CanDelete = inConfiguration && hasPersisted && !_isExecuting;
        Actions.CanCancel = _isExecuting || hasSelected;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static HarvestEntity CloneEntity(HarvestEntity source) => new()
    {
        Id = source.Id,
        Name = source.Name,
        Description = source.Description,
        IsActive = source.IsActive,
        IsDefault = source.IsDefault,
        CreatedAtUtc = source.CreatedAtUtc,
        UpdatedAtUtc = source.UpdatedAtUtc,
        RootPath = source.RootPath,
        OutputPath = source.OutputPath,
        CopyFiles = source.CopyFiles,
        MinScore = source.MinScore,
        IgnoredDirectories = source.IgnoredDirectories.ToArray(),
        IgnoredExtensions = source.IgnoredExtensions.ToArray(),
        IncludedExtensions = source.IncludedExtensions.ToArray(),
        MaxFileSizeKb = source.MaxFileSizeKb,
        FanInWeight = source.FanInWeight,
        FanOutWeight = source.FanOutWeight,
        KeywordDensityWeight = source.KeywordDensityWeight,
        DensityScale = source.DensityScale,
        StaticMethodThreshold = source.StaticMethodThreshold,
        StaticMethodBonus = source.StaticMethodBonus,
        DeadCodePenalty = source.DeadCodePenalty,
        LargeFileThresholdLines = source.LargeFileThresholdLines,
        LargeFilePenalty = source.LargeFilePenalty,
        Categories = source.Categories.Select(c => new HarvestKeywordCategory
        {
            Name = c.Name,
            Weight = c.Weight,
            Keywords = c.Keywords.ToList()
        }).ToList()
    };

    private static IReadOnlyList<string> ParseList(string? input, IReadOnlyList<string> fallback)
    {
        if (string.IsNullOrWhiteSpace(input))
            return fallback;

        var items = input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                         .Where(x => !string.IsNullOrWhiteSpace(x))
                         .ToArray();

        return items.Length > 0 ? items : fallback;
    }

    private static void ApplyFixedDefaults(HarvestEntity entity)
    {
        if (entity.IgnoredDirectories is null || entity.IgnoredDirectories.Count == 0)
            entity.IgnoredDirectories = HarvestDefaults.DefaultIgnoredDirectories;

        if (entity.IgnoredExtensions is null || entity.IgnoredExtensions.Count == 0)
            entity.IgnoredExtensions = HarvestDefaults.DefaultIgnoredExtensions;

        if (entity.IncludedExtensions is null || entity.IncludedExtensions.Count == 0)
            entity.IncludedExtensions = HarvestDefaults.DefaultIncludedExtensions;
    }

    private static string JoinValidationErrors(DevTools.Core.Validation.ValidationResult validationResult)
    {
        if (validationResult.IsValid) return string.Empty;
        return string.Join(" | ", validationResult.Errors.Select(x => x.Message));
    }

    private void ClearInlineValidationStates()
    {
        ValidationUiService.SetControlInvalid(NameInput, false);
        ValidationUiService.SetControlInvalid(DescriptionInput, false);
        ValidationUiService.SetPathSelectorInvalid(RootPathSelector, false);
        ValidationUiService.SetPathSelectorInvalid(OutputPathSelector, false);
        ValidationUiService.SetControlInvalid(IgnoredDirectoriesInput, false);
        ValidationUiService.SetControlInvalid(IgnoredExtensionsInput, false);
        ValidationUiService.SetControlInvalid(IncludedExtensionsInput, false);
        ValidationUiService.SetControlInvalid(MinScoreInput, false);
    }

    private void ApplySelectedConfigurationOption(HarvestSelectionOption? option)
    {
        _currentEntity = option?.Entity is null
            ? CreateUnboundExecutionEntity()
            : option.Entity;

        BindEntityToForm(_currentEntity);
        ValidationUiService.ClearInline(ExecutionStatusText);
        ApplyModeState();
    }

    private HarvestSelectionOption? GetNoConfigurationOption() =>
        _configurationOptions.FirstOrDefault(x => x.Entity is null);

    private void SetSelectedConfigurationOption(HarvestSelectionOption? option)
    {
        _suppressConfigurationSelectionChanged = true;
        try { ConfigurationsCombo.SelectedItem = option; }
        finally { _suppressConfigurationSelectionChanged = false; }
    }

    private static HarvestEntity CreateUnboundExecutionEntity() => new()
    {
        IsActive = true,
        CopyFiles = true,
        IgnoredDirectories = HarvestDefaults.DefaultIgnoredDirectories,
        IgnoredExtensions = HarvestDefaults.DefaultIgnoredExtensions,
        IncludedExtensions = HarvestDefaults.DefaultIncludedExtensions,
    };

    private sealed class HarvestSelectionOption
    {
        public HarvestSelectionOption(string name, HarvestEntity? entity)
        {
            Name = name;
            Entity = entity;
        }

        public string Name { get; }
        public HarvestEntity? Entity { get; }
    }
}
