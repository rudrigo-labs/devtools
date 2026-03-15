using System.Collections.ObjectModel;
using System.Windows;
using DevTools.Host.Wpf.Facades;
using DevTools.Host.Wpf.Services;
using DevTools.Migrations.Models;

namespace DevTools.Host.Wpf.Views;

public partial class MigrationsWorkspaceView : System.Windows.Controls.UserControl
{
    private const string ToolHistorySlug = "migrations";
    private const string ToolDisplayName = "Migrations";
    private const string NoConfigurationOptionLabel = "Configurar manualmente";

    private enum MigrationsWorkspaceMode { Execution, Configuration }

    private readonly ObservableCollection<MigrationsEntity> _entities = new();
    private readonly ObservableCollection<MigrationsSelectionOption> _configurationOptions = new();
    private readonly IMigrationsFacade _facade;
    private MigrationsEntity? _currentEntity;
    private MigrationsWorkspaceMode _currentMode = MigrationsWorkspaceMode.Execution;
    private CancellationTokenSource? _executionCts;
    private bool _isExecuting;
    private bool _initialized;
    private bool _suppressSelectionChanged;
    private bool _isConfigurationDraft;

    public MigrationsWorkspaceView(IMigrationsFacade facade)
    {
        _facade = facade;
        InitializeComponent();

        ActionCombo.ItemsSource = Enum.GetValues<MigrationsAction>();
        ProviderCombo.ItemsSource = Enum.GetValues<DatabaseProvider>();
        ActionCombo.SelectedIndex = 0;
        ProviderCombo.SelectedIndex = 0;
        ActionCombo.SelectionChanged += ActionCombo_SelectionChanged;

        ConfigurationsCombo.ItemsSource = _configurationOptions;
        Loaded += View_Loaded;
        ApplyModeState();
        UpdateMigrationNameVisibility();
    }

    private async void View_Loaded(object sender, RoutedEventArgs e)
    {
        if (_initialized) return;
        _initialized = true;
        await ReloadEntitiesAsync().ConfigureAwait(true);
    }

    public void ActivateExecutionMode()
    {
        if (_isExecuting)
            return;

        if (_currentEntity is null)
            CreateNewEntity();

        _isConfigurationDraft = false;
        SetMode(MigrationsWorkspaceMode.Execution, "Modo execução ativado.");
    }

    public void ActivateConfigurationMode()
    {
        if (_isExecuting)
            return;

        SetMode(MigrationsWorkspaceMode.Configuration, "Modo configuração ativado.");
        ResetConfigurationState();
    }

    // -- Navegação de modo -----------------------------------------------------

    private void SwitchToExecution_Click(object sender, RoutedEventArgs e) => SetMode(MigrationsWorkspaceMode.Execution, "Modo execução ativado.");
    private void SwitchToConfiguration_Click(object sender, RoutedEventArgs e) => SetMode(MigrationsWorkspaceMode.Configuration, "Modo configuração ativado.");

    private void SetMode(MigrationsWorkspaceMode mode, string status)
    {
        if (_isExecuting) return;
        _currentMode = mode;
        ExecutionStatusText.Text = status;
        ApplyModeState();
    }

    // -- Carregamento de entidades ---------------------------------------------

    private async Task ReloadEntitiesAsync()
    {
        var selectedId = _currentEntity?.Id;
        var list = await _facade.LoadAsync();
        _entities.Clear();
        _suppressSelectionChanged = true;

        _configurationOptions.Clear();
        foreach (var item in list) _entities.Add(item);

        if (_entities.Count > 0)
        {
            _configurationOptions.Add(new MigrationsSelectionOption(NoConfigurationOptionLabel, null));
            foreach (var item in _entities)
                _configurationOptions.Add(new MigrationsSelectionOption(item.Name, item));
        }

        _suppressSelectionChanged = false;

        if (_entities.Count == 0)
        {
            SetSelectedOption(null);
            CreateNewEntity();
            ApplyModeState();
            return;
        }

        var toSelect = _entities.FirstOrDefault(x => x.Id == selectedId)
            ?? _entities.FirstOrDefault(x => x.IsDefault)
            ?? _entities.First();

        SetSelectedOption(toSelect);
        BindEntityToForm(toSelect);
        ApplyModeState();
    }

    private void SetSelectedOption(MigrationsEntity? entity)
    {
        _suppressSelectionChanged = true;
        ConfigurationsCombo.SelectedItem = entity is null
            ? null
            : _configurationOptions.FirstOrDefault(o => o.Entity?.Id == entity.Id);
        _suppressSelectionChanged = false;
    }

    private async void ConfigurationsCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_suppressSelectionChanged || _isExecuting) return;
        if (ConfigurationsCombo.SelectedItem is not MigrationsSelectionOption opt) return;

        if (opt.Entity is null)
        {
            CreateNewEntity();
            _isConfigurationDraft = false;
            ApplyModeState();
            return;
        }

        _currentEntity = opt.Entity;
        BindEntityToForm(_currentEntity);
        ExecutionStatusText.Text = $"Configuração \"{_currentEntity.Name}\" carregada.";
        if (_currentMode == MigrationsWorkspaceMode.Configuration)
            _isConfigurationDraft = true;
        ApplyModeState();
    }

    // -- CRUD -----------------------------------------------------------------

    private void ActionNew_Click(object sender, RoutedEventArgs e)
    {
        if (_isExecuting) return;

        if (_currentMode == MigrationsWorkspaceMode.Execution)
        {
            SetMode(MigrationsWorkspaceMode.Configuration, "Modo configuração ativado.");
            ResetConfigurationState();
            return;
        }

        _isConfigurationDraft = true;
        CreateNewEntity();
        SetMode(MigrationsWorkspaceMode.Configuration, "Nova configuração.");
    }

    private async void ActionSave_Click(object sender, RoutedEventArgs e)
    {
        if (_isExecuting) return;

        if (_currentMode == MigrationsWorkspaceMode.Execution)
        {
            await ExecuteAsync().ConfigureAwait(true);
            return;
        }

        if (!_isConfigurationDraft)
        {
            ValidationUiService.ShowInline(ExecutionStatusText, "Clique em Novo para iniciar uma configuração.");
            return;
        }

        ReadFormIntoEntity();

        ValidationUiService.SetControlInvalid(NameInput, false);
        ValidationUiService.SetPathSelectorInvalid(RootPathSelector, false);
        ValidationUiService.SetPathSelectorInvalid(StartupProjectSelector, false);
        ValidationUiService.SetControlInvalid(DbContextInput, false);

        if (!ValidationUiService.ValidateRequiredFields(out var err,
            ValidationUiService.RequiredControl("Nome", NameInput, NameInput.Text),
            ValidationUiService.RequiredPath("Pasta raiz", RootPathSelector, RootPathSelector.SelectedPath),
            ValidationUiService.RequiredPath("Startup project", StartupProjectSelector, StartupProjectSelector.SelectedPath),
            ValidationUiService.RequiredControl("DbContext", DbContextInput, DbContextInput.Text)))
        {
            ValidationUiService.ShowInline(ExecutionStatusText, err);
            return;
        }

        var validation = await _facade.SaveAsync(_currentEntity!).ConfigureAwait(true);
        if (!validation.IsValid)
        {
            ValidationUiService.ShowInline(ExecutionStatusText,
                string.Join(" | ", validation.Errors.Select(x => x.Message)));
            return;
        }

        ValidationUiService.ClearInline(ExecutionStatusText);
        await ReloadEntitiesAsync().ConfigureAwait(true);
        ExecutionStatusText.Text = "Configuração salva.";
        ResetConfigurationState();
    }

    private async void ActionDelete_Click(object sender, RoutedEventArgs e)
    {
        if (_currentMode != MigrationsWorkspaceMode.Configuration)
            return;

        if (_isExecuting || _currentEntity is null || string.IsNullOrWhiteSpace(_currentEntity.Id)) return;

        var confirm = Components.DevToolsMessageBox.Confirm(
            Window.GetWindow(this),
            $"Excluir configuração \"{_currentEntity.Name}\"?",
            "Excluir");

        if (confirm != Components.DevToolsMessageBoxResult.Yes) return;

        await _facade.DeleteAsync(_currentEntity.Id).ConfigureAwait(true);
        _currentEntity = null;
        await ReloadEntitiesAsync().ConfigureAwait(true);
        ExecutionStatusText.Text = "Configuração excluída.";
    }

    private void ActionCancel_Click(object sender, RoutedEventArgs e)
    {
        if (_currentMode == MigrationsWorkspaceMode.Configuration)
        {
            ResetConfigurationState();
            ExecutionStatusText.Text = "Configuração cancelada.";
            return;
        }

        ActionBack_Click(sender, e);
    }

    private void ActionGoToTool_Click(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is MainWindow mainWindow)
        {
            mainWindow.OpenToolExecution("Migrations");
            return;
        }

        SetMode(MigrationsWorkspaceMode.Execution, "Modo execução ativado.");
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

    // -- Execução -------------------------------------------------------------

    private async Task ExecuteAsync()
    {
        if (_isExecuting) return;

        if (_currentEntity is null || string.IsNullOrWhiteSpace(_currentEntity.RootPath))
        {
            ValidationUiService.ShowInline(ExecutionStatusText, "Selecione ou configure uma configuração antes de executar.");
            return;
        }

        var action = ActionCombo.SelectedItem is MigrationsAction a ? a : MigrationsAction.AddMigration;
        var provider = ProviderCombo.SelectedItem is DatabaseProvider p ? p : DatabaseProvider.Sqlite;
        var migrationName = MigrationNameInput.Text.Trim();

        if (action == MigrationsAction.AddMigration && string.IsNullOrWhiteSpace(migrationName))
        {
            ValidationUiService.SetControlInvalid(MigrationNameInput, true);
            ValidationUiService.ShowInline(ExecutionStatusText, "Nome da migration é obrigatório para AddMigration.");
            return;
        }

        var request = new MigrationsRequest
        {
            Action        = action,
            Provider      = provider,
            Settings      = _currentEntity,
            MigrationName = migrationName,
            DryRun        = DryRunCheck.IsChecked ?? false
        };

        await ToolHistoryViewHelper.RecordAsync(ToolHistorySlug, WorkspaceRoot, $"Executar migration ({action})").ConfigureAwait(true);

        _executionCts?.Dispose();
        _executionCts = new CancellationTokenSource();
        _isExecuting = true;
        ResultPanel.Visibility = Visibility.Collapsed;
        ApplyModeState();
        ExecutionStatusText.Text = request.DryRun ? "Montando comando..." : "Executando dotnet ef...";

        try
        {
            var result = await _facade.ExecuteAsync(request, _executionCts.Token).ConfigureAwait(true);

            if (!result.IsSuccess)
            {
                ValidationUiService.ShowInline(ExecutionStatusText,
                    string.Join(" | ", result.Errors.Select(x => x.Message)));

                if (result.Value is not null)
                    ShowResult(result.Value);

                return;
            }

            ValidationUiService.ClearInline(ExecutionStatusText);
            ShowResult(result.Value!);

            ExecutionStatusText.Text = request.DryRun
                ? "Dry run  -  comando gerado."
                : $"Concluído. Exit code: {result.Value!.ExitCode}";
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

    private void ShowResult(MigrationsResult r)
    {
        ResultCommandText.Text = $"> {r.Command}";
        var output = string.Join("\n\n", new[] { r.StdOut?.Trim(), r.StdErr?.Trim() }
            .Where(s => !string.IsNullOrWhiteSpace(s)));
        ResultOutputText.Text = output;
        ResultOutputText.Visibility = string.IsNullOrWhiteSpace(output) ? Visibility.Collapsed : Visibility.Visible;
        ResultPanel.Visibility = Visibility.Visible;
    }

    // -- Binding ---------------------------------------------------------------

    private void BindEntityToForm(MigrationsEntity entity)
    {
        _currentEntity = entity;
        NameInput.Text = entity.Name;
        RootPathSelector.SelectedPath = entity.RootPath;
        StartupProjectSelector.SelectedPath = entity.StartupProjectPath;
        DbContextInput.Text = entity.DbContextFullName;
        AdditionalArgsInput.Text = entity.AdditionalArgs ?? string.Empty;
        IsDefaultCheck.IsChecked = entity.IsDefault;

        var sqlServer = entity.Targets.FirstOrDefault(t => t.Provider == DatabaseProvider.SqlServer);
        var sqlite = entity.Targets.FirstOrDefault(t => t.Provider == DatabaseProvider.Sqlite);
        SqlServerProjectInput.Text = sqlServer?.MigrationsProjectPath ?? string.Empty;
        SqliteProjectInput.Text = sqlite?.MigrationsProjectPath ?? string.Empty;
    }

    private void ReadFormIntoEntity()
    {
        if (_currentEntity is null) return;
        _currentEntity.Name = NameInput.Text.Trim();
        _currentEntity.RootPath = RootPathSelector.SelectedPath?.Trim() ?? string.Empty;
        _currentEntity.StartupProjectPath = StartupProjectSelector.SelectedPath?.Trim() ?? string.Empty;
        _currentEntity.DbContextFullName = DbContextInput.Text.Trim();
        _currentEntity.AdditionalArgs = string.IsNullOrWhiteSpace(AdditionalArgsInput.Text) ? null : AdditionalArgsInput.Text.Trim();
        _currentEntity.IsDefault = IsDefaultCheck.IsChecked ?? false;

        _currentEntity.Targets.Clear();
        if (!string.IsNullOrWhiteSpace(SqlServerProjectInput.Text))
            _currentEntity.Targets.Add(new MigrationTarget { Provider = DatabaseProvider.SqlServer, MigrationsProjectPath = SqlServerProjectInput.Text.Trim() });
        if (!string.IsNullOrWhiteSpace(SqliteProjectInput.Text))
            _currentEntity.Targets.Add(new MigrationTarget { Provider = DatabaseProvider.Sqlite, MigrationsProjectPath = SqliteProjectInput.Text.Trim() });
    }

    private void CreateNewEntity()
    {
        _currentEntity = new MigrationsEntity();
        BindEntityToForm(_currentEntity);
        SetSelectedOption(null);
        ApplyModeState();
    }

    private void ResetConfigurationState()
    {
        _isConfigurationDraft = false;
        CreateNewEntity();
        ValidationUiService.ClearInline(ExecutionStatusText);
        ApplyModeState();
    }

    private void ActionCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        => UpdateMigrationNameVisibility();

    private void UpdateMigrationNameVisibility()
    {
        MigrationNamePanel.Visibility = ActionCombo.SelectedItem is MigrationsAction.AddMigration
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void ApplyModeState()
    {
        var inConfiguration = _currentMode == MigrationsWorkspaceMode.Configuration;
        var inExecution = _currentMode == MigrationsWorkspaceMode.Execution;
        var hasSelected = _currentEntity is not null;

        ConfigurationModeHint.Visibility = inConfiguration ? Visibility.Visible : Visibility.Collapsed;
        ConfigurationMetadataSection.Visibility = inConfiguration ? Visibility.Visible : Visibility.Collapsed;

        WorkspaceTitleText.Text = inConfiguration ? "Migrations - Configuração" : "Migrations";
        WorkspaceSubtitleText.Text = inConfiguration
            ? "Salve os parâmetros de migration para reaproveitar em outros ciclos."
            : "Executa dotnet ef migrations add e database update com configurações nomeadas por provedor.";

        Actions.NewText = "Novo";
        Actions.SaveText = inConfiguration ? "Salvar" : "Executar";
        Actions.SaveIconKind = inConfiguration ? "ContentSave" : "Play";
        Actions.CancelText = "Cancelar";
        Actions.GoToToolText = "Ir para ferramenta";
        Actions.BackText = _isExecuting ? "Cancelar" : "Voltar";
        Actions.BackIconKind = _isExecuting ? "CloseCircleOutline" : "ArrowLeft";

        Actions.ShowHelp = true;
        Actions.HelpContextKey = inConfiguration ? "migrations:configuration" : "migrations:execution";
        Actions.ShowNew = inConfiguration;
        Actions.ShowSave = inConfiguration || inExecution;
        Actions.ShowDelete = false;
        Actions.ShowCancel = inConfiguration;
        Actions.ShowGoToTool = inConfiguration;
        Actions.ShowBack = inExecution;

        Actions.CanHelp = true;
        Actions.CanNew = inConfiguration && !_isExecuting;
        Actions.CanSave = !_isExecuting && (inExecution ? hasSelected : _isConfigurationDraft);
        Actions.CanDelete = false;
        Actions.CanCancel = inConfiguration && !_isExecuting && _isConfigurationDraft;
        Actions.CanGoToTool = inConfiguration && !_isExecuting;
        Actions.CanBack = inExecution;
    }
}

