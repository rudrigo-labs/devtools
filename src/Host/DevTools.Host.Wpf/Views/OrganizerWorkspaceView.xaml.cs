using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using DevTools.Host.Wpf.Facades;
using DevTools.Host.Wpf.Services;
using DevTools.Organizer.Models;

namespace DevTools.Host.Wpf.Views;

public partial class OrganizerWorkspaceView : System.Windows.Controls.UserControl
{
    private const string ToolHistorySlug = "organizer";
    private const string ToolDisplayName = "Organizer";
    private const string NoConfigurationOptionLabel = "Configurar manualmente";

    private enum OrganizerWorkspaceMode
    {
        Execution,
        Configuration
    }

    private readonly ObservableCollection<OrganizerEntity> _entities = new();
    private readonly ObservableCollection<OrganizerSelectionOption> _configurationOptions = new();
    private readonly ObservableCollection<OrganizerCategory> _workingCategories = new();
    private readonly IOrganizerFacade _facade;

    private OrganizerEntity? _currentEntity;
    private OrganizerWorkspaceMode _currentMode = OrganizerWorkspaceMode.Execution;
    private CancellationTokenSource? _executionCts;
    private bool _isExecuting;
    private bool _initialized;
    private bool _suppressConfigurationSelectionChanged;
    private bool _isConfigurationDraft;
    private int _selectedCategoryIndex = -1;

    public OrganizerWorkspaceView(IOrganizerFacade facade)
    {
        _facade = facade;
        InitializeComponent();
        ConfigurationsCombo.ItemsSource = _configurationOptions;
        CategoriesList.ItemsSource = _workingCategories;
        Loaded += OrganizerWorkspaceView_Loaded;
        ApplyModeState();
        UpdateCategoriesCount();
    }

    private async void OrganizerWorkspaceView_Loaded(object sender, RoutedEventArgs e)
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
        SetMode(OrganizerWorkspaceMode.Execution, "Modo execução ativado.");
    }

    public void ActivateConfigurationMode()
    {
        if (_isExecuting)
            return;

        SetMode(OrganizerWorkspaceMode.Configuration, "Modo configuração ativado.");
        ResetConfigurationState();
    }

    private async Task ReloadEntitiesAsync()
    {
        var selectedId = _currentEntity?.Id;
        var list = await _facade.LoadAsync().ConfigureAwait(true);

        _entities.Clear();
        foreach (var item in list)
        {
            ApplyFixedDefaults(item);
            _entities.Add(item);
        }

        _configurationOptions.Clear();
        if (_entities.Count > 0)
        {
            _configurationOptions.Add(new OrganizerSelectionOption(NoConfigurationOptionLabel, null));
            foreach (var item in _entities)
                _configurationOptions.Add(new OrganizerSelectionOption(item.Name, item));
        }

        if (_entities.Count == 0)
        {
            SetSelectedConfigurationOption(null);
            CreateNewEntity();
            return;
        }

        OrganizerEntity? selected = null;
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

        if (ConfigurationsCombo.SelectedItem is not OrganizerSelectionOption option)
            return;

        ApplySelectedConfigurationOption(option);
        if (_currentMode == OrganizerWorkspaceMode.Configuration)
        {
            _isConfigurationDraft = option.Entity is not null;
            ApplyModeState();
        }
    }

    private async void ActionSave_Click(object sender, RoutedEventArgs e)
    {
        if (_isExecuting)
            return;

        if (_currentMode == OrganizerWorkspaceMode.Execution)
        {
            await ExecuteCurrentAsync().ConfigureAwait(true);
            return;
        }

        if (!_isConfigurationDraft)
        {
            ValidationUiService.ShowInline(ExecutionStatusText, "Clique em Novo para iniciar uma configuração.");
            return;
        }

        if (!TryBindFormToEntity(out var entity, out var validationError, requireName: true, requireInboxPath: false))
        {
            ValidationUiService.ShowInline(ExecutionStatusText, validationError);
            return;
        }

        var result = await _facade.SaveAsync(entity).ConfigureAwait(true);
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
        if (_isExecuting || _currentMode != OrganizerWorkspaceMode.Configuration)
            return;

        if (_currentEntity is null)
            return;

        if (string.IsNullOrWhiteSpace(_currentEntity.Id))
        {
            CreateNewEntity();
            return;
        }

        await _facade.DeleteAsync(_currentEntity.Id).ConfigureAwait(true);
        ValidationUiService.ClearInline(ExecutionStatusText);
        await ReloadEntitiesAsync().ConfigureAwait(true);
        ExecutionStatusText.Text = "Configuração removida.";
        SetMode(OrganizerWorkspaceMode.Execution);
    }

    private void ActionNew_Click(object sender, RoutedEventArgs e)
    {
        if (_isExecuting)
            return;

        if (_currentMode == OrganizerWorkspaceMode.Execution)
        {
            SetMode(OrganizerWorkspaceMode.Configuration, "Modo configuração ativado.");
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
        if (_currentMode == OrganizerWorkspaceMode.Configuration)
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
            mainWindow.OpenToolExecution("Organizer");
            return;
        }

        SetMode(OrganizerWorkspaceMode.Execution, "Modo execução ativado.");
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

    private async void HistoryButton_Click(object sender, RoutedEventArgs e)
        => await ToolHistoryViewHelper.ShowAndApplyAsync(WorkspaceRoot, ToolHistorySlug, ToolDisplayName, ExecutionStatusText).ConfigureAwait(true);

    private async void ExecuteNowButton_Click(object sender, RoutedEventArgs e)
        => await ExecuteCurrentAsync().ConfigureAwait(true);

    private async Task ExecuteCurrentAsync()
    {
        if (_isExecuting)
            return;

        if (!TryBindFormToEntity(out var entity, out var validationError, requireName: false, requireInboxPath: true))
        {
            ValidationUiService.ShowInline(ExecutionStatusText, validationError);
            return;
        }

        var request = BuildRequest(entity);
        var apply = request.Apply;
        await ToolHistoryViewHelper.RecordAsync(ToolHistorySlug, WorkspaceRoot, "Executar organização").ConfigureAwait(true);

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
            var stats = data.Stats;

            ResultSummaryText.Text =
                $"Total: {stats.TotalFiles} | Elegíveis: {stats.EligibleFiles} | " +
                $"Movidos: {stats.WouldMove} | Duplicatas: {stats.Duplicates} | " +
                $"Ignorados: {stats.Ignored} | Erros: {stats.Errors}";

            ResultsList.ItemsSource = data.Plan
                .Where(p => p.Action is OrganizerAction.WouldMove or OrganizerAction.Moved or OrganizerAction.Duplicate)
                .ToList();

            ResultPanel.Visibility = Visibility.Visible;

            var mode = apply ? "Execução" : "Simulação";
            ExecutionStatusText.Text = $"{mode} concluída. {stats.WouldMove} arquivo(s) classificado(s).";
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

    private void AddCategoryButton_Click(object sender, RoutedEventArgs e)
    {
        CommitSelectedCategoryEdits();

        var nextIndex = _workingCategories.Count + 1;
        var name = $"Categoria{nextIndex}";
        var category = new OrganizerCategory
        {
            Name = name,
            Folder = name,
            Keywords = ["palavra-chave"],
            NegativeKeywords = Array.Empty<string>(),
            KeywordWeight = 2,
            NegativeWeight = 2,
            MinScore = null
        };

        _workingCategories.Add(category);
        CategoriesList.SelectedItem = category;
        ApplyCategoryEditorState();
    }

    private void RemoveCategoryButton_Click(object sender, RoutedEventArgs e)
    {
        if (CategoriesList.SelectedItem is not OrganizerCategory selected)
            return;

        var index = CategoriesList.SelectedIndex;
        _workingCategories.Remove(selected);

        if (_workingCategories.Count == 0)
        {
            _selectedCategoryIndex = -1;
            ClearCategoryEditor();
            ApplyCategoryEditorState();
            return;
        }

        var targetIndex = Math.Max(0, index - 1);
        CategoriesList.SelectedIndex = targetIndex;
        ApplyCategoryEditorState();
    }

    private void CategoriesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        CommitSelectedCategoryEdits();
        _selectedCategoryIndex = CategoriesList.SelectedIndex;
        BindSelectedCategoryToForm();
        ApplyCategoryEditorState();
    }

    private void CommitSelectedCategoryEdits()
    {
        if (_selectedCategoryIndex < 0 || _selectedCategoryIndex >= _workingCategories.Count)
            return;

        var selected = _workingCategories[_selectedCategoryIndex];
        selected.Name = CategoryNameInput.Text.Trim();
        selected.Folder = CategoryFolderInput.Text.Trim();
        selected.Keywords = ParseList(CategoryKeywordsInput.Text, Array.Empty<string>()).ToArray();
        selected.NegativeKeywords = ParseList(CategoryNegativeKeywordsInput.Text, Array.Empty<string>()).ToArray();
        selected.KeywordWeight = ParseInt(CategoryKeywordWeightInput.Text, 2);
        selected.NegativeWeight = ParseInt(CategoryNegativeWeightInput.Text, 2);
        selected.MinScore = ParseNullableInt(CategoryMinScoreInput.Text);

        CategoriesList.Items.Refresh();
    }

    private void BindSelectedCategoryToForm()
    {
        if (CategoriesList.SelectedItem is not OrganizerCategory selected)
        {
            ClearCategoryEditor();
            return;
        }

        CategoryNameInput.Text = selected.Name;
        CategoryFolderInput.Text = selected.Folder;
        CategoryKeywordsInput.Text = string.Join(", ", selected.Keywords);
        CategoryNegativeKeywordsInput.Text = string.Join(", ", selected.NegativeKeywords);
        CategoryKeywordWeightInput.Text = selected.KeywordWeight.ToString(CultureInfo.InvariantCulture);
        CategoryNegativeWeightInput.Text = selected.NegativeWeight.ToString(CultureInfo.InvariantCulture);
        CategoryMinScoreInput.Text = selected.MinScore?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private void ApplyCategoryEditorState()
    {
        var hasSelected = CategoriesList.SelectedItem is OrganizerCategory;
        RemoveCategoryButton.IsEnabled = hasSelected;
        CategoryEditorPanel.IsEnabled = hasSelected;
        UpdateCategoriesCount();
    }

    private void UpdateCategoriesCount()
    {
        var count = _workingCategories.Count;
        CategoriesCountText.Text = count == 1
            ? "1 categoria configurada"
            : $"{count} categorias configuradas";

        if (count > 0)
            ValidationUiService.SetControlInvalid(CategoriesList, false);
    }

    private void ClearCategoryEditor()
    {
        CategoryNameInput.Text = string.Empty;
        CategoryFolderInput.Text = string.Empty;
        CategoryKeywordsInput.Text = string.Empty;
        CategoryNegativeKeywordsInput.Text = string.Empty;
        CategoryKeywordWeightInput.Text = "2";
        CategoryNegativeWeightInput.Text = "2";
        CategoryMinScoreInput.Text = string.Empty;
    }

    private void CreateNewEntity()
    {
        _currentEntity = new OrganizerEntity
        {
            Name = "Organizer 1",
            Description = string.Empty,
            IsActive = true,
            MinScore = 3,
            Apply = false,
            AllowedExtensions = OrganizerDefaults.DefaultAllowedExtensions(),
            Categories = OrganizerDefaults.DefaultCategories(),
            FileNameWeight = OrganizerDefaults.DefaultFileNameWeight,
            DeduplicateByHash = OrganizerDefaults.DefaultDeduplicateByHash,
            DeduplicateByName = OrganizerDefaults.DefaultDeduplicateByName,
            DeduplicateFirstLines = OrganizerDefaults.DefaultDeduplicateFirstLines,
            DuplicatesFolderName = OrganizerDefaults.DefaultDuplicatesFolderName,
            OthersFolderName = OrganizerDefaults.DefaultOthersFolderName
        };

        SetSelectedConfigurationOption(GetNoConfigurationOption());
        BindEntityToForm(_currentEntity);
        ApplyModeState();
    }

    private void BindEntityToForm(OrganizerEntity entity)
    {
        ApplyFixedDefaults(entity);
        _selectedCategoryIndex = -1;

        NameInput.Text = entity.Name;
        DescriptionInput.Text = entity.Description;
        InboxPathSelector.SelectedPath = entity.InboxPath;
        OutputPathSelector.SelectedPath = entity.OutputPath;
        MinScoreInput.Text = entity.MinScore.ToString(CultureInfo.InvariantCulture);
        ApplyCheck.IsChecked = entity.Apply;
        AllowedExtensionsInput.Text = string.Join(", ", entity.AllowedExtensions);
        FileNameWeightInput.Text = entity.FileNameWeight.ToString(CultureInfo.InvariantCulture);
        DeduplicateByNameCheck.IsChecked = entity.DeduplicateByName;
        DeduplicateByHashCheck.IsChecked = entity.DeduplicateByHash;
        DeduplicateFirstLinesInput.Text = entity.DeduplicateFirstLines.ToString(CultureInfo.InvariantCulture);
        DuplicatesFolderInput.Text = entity.DuplicatesFolderName;
        OthersFolderInput.Text = entity.OthersFolderName;

        _workingCategories.Clear();
        foreach (var category in CloneCategories(entity.Categories))
            _workingCategories.Add(category);

        if (_workingCategories.Count > 0)
        {
            CategoriesList.SelectedIndex = 0;
        }
        else
        {
            _selectedCategoryIndex = -1;
            ClearCategoryEditor();
        }

        ApplyCategoryEditorState();
    }

    private bool TryBindFormToEntity(
        out OrganizerEntity entity,
        out string errorMessage,
        bool requireName,
        bool requireInboxPath)
    {
        entity = _currentEntity is null ? new OrganizerEntity() : CloneEntity(_currentEntity);

        ClearInlineValidationStates();
        CommitSelectedCategoryEdits();

        var requiredFields = new List<ValidationUiService.RequiredField>();
        if (requireName)
        {
            requiredFields.Add(ValidationUiService.RequiredControl("Nome", NameInput, NameInput.Text));
            requiredFields.Add(ValidationUiService.RequiredControl("Descrição", DescriptionInput, DescriptionInput.Text));
        }

        if (requireInboxPath)
            requiredFields.Add(ValidationUiService.RequiredPath("Pasta de entrada", InboxPathSelector, InboxPathSelector.SelectedPath));

        if (!ValidationUiService.ValidateRequiredFields(out errorMessage, requiredFields.ToArray()))
            return false;

        if (_workingCategories.Count == 0)
        {
            errorMessage = "Adicione ao menos uma categoria.";
            ValidationUiService.SetControlInvalid(CategoriesList, true);
            return false;
        }

        var alloweds = ParseExtensions(AllowedExtensionsInput.Text);
        if (alloweds.Count == 0)
        {
            errorMessage = "Informe ao menos uma extensão permitida.";
            ValidationUiService.SetControlInvalid(AllowedExtensionsInput, true);
            return false;
        }

        var name = NameInput.Text.Trim();
        entity.Name = requireName
            ? name
            : (string.IsNullOrWhiteSpace(name) ? entity.Name : name);

        entity.Description = DescriptionInput.Text.Trim();
        entity.InboxPath = InboxPathSelector.SelectedPath?.Trim() ?? string.Empty;
        entity.OutputPath = OutputPathSelector.SelectedPath?.Trim() ?? string.Empty;
        entity.MinScore = ParseInt(MinScoreInput.Text, 3);
        entity.Apply = ApplyCheck.IsChecked ?? false;
        entity.AllowedExtensions = alloweds.ToArray();
        entity.FileNameWeight = ParseDouble(FileNameWeightInput.Text, OrganizerDefaults.DefaultFileNameWeight);
        entity.DeduplicateByName = DeduplicateByNameCheck.IsChecked ?? OrganizerDefaults.DefaultDeduplicateByName;
        entity.DeduplicateByHash = DeduplicateByHashCheck.IsChecked ?? OrganizerDefaults.DefaultDeduplicateByHash;
        entity.DeduplicateFirstLines = ParseInt(DeduplicateFirstLinesInput.Text, OrganizerDefaults.DefaultDeduplicateFirstLines);
        entity.DuplicatesFolderName = string.IsNullOrWhiteSpace(DuplicatesFolderInput.Text)
            ? OrganizerDefaults.DefaultDuplicatesFolderName
            : DuplicatesFolderInput.Text.Trim();
        entity.OthersFolderName = string.IsNullOrWhiteSpace(OthersFolderInput.Text)
            ? OrganizerDefaults.DefaultOthersFolderName
            : OthersFolderInput.Text.Trim();
        entity.Categories = CloneCategories(_workingCategories);
        entity.IsActive = true;

        _currentEntity = entity;
        errorMessage = string.Empty;
        return true;
    }

    private static OrganizerRequest BuildRequest(OrganizerEntity source) => new()
    {
        InboxPath = source.InboxPath,
        OutputPath = source.OutputPath,
        MinScore = source.MinScore,
        Apply = source.Apply,
        AllowedExtensions = (source.AllowedExtensions?.Length > 0
            ? source.AllowedExtensions
            : OrganizerDefaults.DefaultAllowedExtensions()).ToArray(),
        FileNameWeight = source.FileNameWeight,
        DeduplicateByHash = source.DeduplicateByHash,
        DeduplicateByName = source.DeduplicateByName,
        DeduplicateFirstLines = source.DeduplicateFirstLines,
        DuplicatesFolderName = source.DuplicatesFolderName,
        OthersFolderName = source.OthersFolderName,
        Categories = CloneCategories(source.Categories ?? OrganizerDefaults.DefaultCategories())
    };

    private void SetMode(OrganizerWorkspaceMode mode, string? statusMessage = null)
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
        var inConfiguration = _currentMode == OrganizerWorkspaceMode.Configuration;
        var inExecution = _currentMode == OrganizerWorkspaceMode.Execution;

        ConfigurationsLabel.Visibility = hasConfigurations ? Visibility.Visible : Visibility.Collapsed;
        ConfigurationsCombo.Visibility = hasConfigurations ? Visibility.Visible : Visibility.Collapsed;
        ConfigurationMetadataSection.Visibility = inConfiguration ? Visibility.Visible : Visibility.Collapsed;
        ConfigurationModeHint.Visibility = Visibility.Collapsed;
        ExecuteNowButton.Visibility = inExecution ? Visibility.Visible : Visibility.Collapsed;

        WorkspaceTitleText.Text = inConfiguration ? "Organizer - Configuração" : "Organizer";
        WorkspaceSubtitleText.Text = inConfiguration
            ? "Defina regras, categorias e metadados para salvar uma configuração reutilizável."
            : "Organiza documentos por categorias usando palavras-chave e deduplicação.";

        Actions.NewText = "Novo";
        Actions.SaveText = inConfiguration ? "Salvar" : "Executar";
        Actions.SaveIconKind = inConfiguration ? "ContentSave" : "Play";
        Actions.CancelText = "Cancelar";
        Actions.GoToToolText = "Ir para ferramenta";
        Actions.BackText = _isExecuting ? "Cancelar execução" : "Voltar";
        Actions.BackIconKind = _isExecuting ? "CloseCircleOutline" : "ArrowLeft";

        Actions.ShowHelp = true;
        Actions.ShowHistory = inExecution;
        Actions.HelpContextKey = "organizer:execution";
        Actions.ShowNew = inConfiguration;
        Actions.ShowSave = inConfiguration || inExecution;
        Actions.ShowDelete = inConfiguration;
        Actions.ShowCancel = inConfiguration;
        Actions.ShowGoToTool = false;
        Actions.ShowBack = inExecution;

        Actions.CanHelp = true;
        Actions.CanNew = inConfiguration && !_isExecuting && !_isConfigurationDraft;
        Actions.CanSave = !_isExecuting && (inExecution ? hasSelected : _isConfigurationDraft);
        Actions.CanDelete = inConfiguration && !_isExecuting && _isConfigurationDraft;
        Actions.CanCancel = inConfiguration && !_isExecuting && _isConfigurationDraft;
        Actions.CanGoToTool = false;
        Actions.CanBack = inExecution;
    }

    private static OrganizerEntity CloneEntity(OrganizerEntity source) => new()
    {
        Id = source.Id,
        Name = source.Name,
        Description = source.Description,
        IsActive = source.IsActive,
        IsDefault = source.IsDefault,
        CreatedAtUtc = source.CreatedAtUtc,
        UpdatedAtUtc = source.UpdatedAtUtc,
        InboxPath = source.InboxPath,
        OutputPath = source.OutputPath,
        MinScore = source.MinScore,
        Apply = source.Apply,
        AllowedExtensions = (source.AllowedExtensions ?? OrganizerDefaults.DefaultAllowedExtensions()).ToArray(),
        FileNameWeight = source.FileNameWeight,
        DeduplicateByHash = source.DeduplicateByHash,
        DeduplicateByName = source.DeduplicateByName,
        DeduplicateFirstLines = source.DeduplicateFirstLines,
        DuplicatesFolderName = source.DuplicatesFolderName,
        OthersFolderName = source.OthersFolderName,
        Categories = CloneCategories(source.Categories ?? OrganizerDefaults.DefaultCategories())
    };

    private static List<OrganizerCategory> CloneCategories(IEnumerable<OrganizerCategory> source) =>
        source.Select(c => new OrganizerCategory
        {
            Name = c.Name,
            Folder = c.Folder,
            Keywords = c.Keywords.ToArray(),
            NegativeKeywords = c.NegativeKeywords.ToArray(),
            KeywordWeight = c.KeywordWeight,
            NegativeWeight = c.NegativeWeight,
            MinScore = c.MinScore
        }).ToList();

    private static IReadOnlyList<string> ParseList(string? input, IReadOnlyList<string> fallback)
    {
        if (string.IsNullOrWhiteSpace(input))
            return fallback;

        var items = input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return items.Length > 0 ? items : fallback;
    }

    private static List<string> ParseExtensions(string? input)
    {
        var items = ParseList(input, Array.Empty<string>());
        return items
            .Select(x => x.StartsWith(".", StringComparison.Ordinal) ? x : "." + x)
            .Select(x => x.ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static int ParseInt(string? value, int fallback)
        => int.TryParse(value?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? Math.Max(0, parsed)
            : fallback;

    private static int? ParseNullableInt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? Math.Max(0, parsed)
            : null;
    }

    private static double ParseDouble(string? value, double fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        var text = value.Trim();
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out var parsedCurrent))
            return parsedCurrent > 0 ? parsedCurrent : fallback;

        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedInvariant))
            return parsedInvariant > 0 ? parsedInvariant : fallback;

        return fallback;
    }

    private static void ApplyFixedDefaults(OrganizerEntity entity)
    {
        if (entity.AllowedExtensions is null || entity.AllowedExtensions.Length == 0)
            entity.AllowedExtensions = OrganizerDefaults.DefaultAllowedExtensions();

        if (entity.Categories is null || entity.Categories.Count == 0)
            entity.Categories = OrganizerDefaults.DefaultCategories();

        if (entity.FileNameWeight <= 0)
            entity.FileNameWeight = OrganizerDefaults.DefaultFileNameWeight;

        if (string.IsNullOrWhiteSpace(entity.DuplicatesFolderName))
            entity.DuplicatesFolderName = OrganizerDefaults.DefaultDuplicatesFolderName;

        if (string.IsNullOrWhiteSpace(entity.OthersFolderName))
            entity.OthersFolderName = OrganizerDefaults.DefaultOthersFolderName;
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
        ValidationUiService.SetPathSelectorInvalid(InboxPathSelector, false);
        ValidationUiService.SetControlInvalid(AllowedExtensionsInput, false);
        ValidationUiService.SetControlInvalid(CategoriesList, false);
    }

    private void ApplySelectedConfigurationOption(OrganizerSelectionOption? option)
    {
        _currentEntity = option?.Entity is null
            ? CreateUnboundExecutionEntity()
            : CloneEntity(option.Entity);

        BindEntityToForm(_currentEntity);
        ValidationUiService.ClearInline(ExecutionStatusText);
        if (_currentMode == OrganizerWorkspaceMode.Configuration)
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

    private OrganizerSelectionOption? GetNoConfigurationOption() =>
        _configurationOptions.FirstOrDefault(x => x.Entity is null);

    private void SetSelectedConfigurationOption(OrganizerSelectionOption? option)
    {
        _suppressConfigurationSelectionChanged = true;
        try { ConfigurationsCombo.SelectedItem = option; }
        finally { _suppressConfigurationSelectionChanged = false; }
    }

    private static OrganizerEntity CreateUnboundExecutionEntity() => new()
    {
        IsActive = true,
        MinScore = 3,
        Apply = false,
        AllowedExtensions = OrganizerDefaults.DefaultAllowedExtensions(),
        Categories = OrganizerDefaults.DefaultCategories(),
        FileNameWeight = OrganizerDefaults.DefaultFileNameWeight,
        DeduplicateByHash = OrganizerDefaults.DefaultDeduplicateByHash,
        DeduplicateByName = OrganizerDefaults.DefaultDeduplicateByName,
        DeduplicateFirstLines = OrganizerDefaults.DefaultDeduplicateFirstLines,
        DuplicatesFolderName = OrganizerDefaults.DefaultDuplicatesFolderName,
        OthersFolderName = OrganizerDefaults.DefaultOthersFolderName
    };

    private sealed class OrganizerSelectionOption
    {
        public OrganizerSelectionOption(string name, OrganizerEntity? entity)
        {
            Name = name;
            Entity = entity;
        }

        public string Name { get; }
        public OrganizerEntity? Entity { get; }
    }
}

