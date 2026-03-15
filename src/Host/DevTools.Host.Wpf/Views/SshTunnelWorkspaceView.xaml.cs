using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using DevTools.Host.Wpf.Facades;
using DevTools.Host.Wpf.Services;
using DevTools.SSHTunnel.Models;

namespace DevTools.Host.Wpf.Views;

public partial class SshTunnelWorkspaceView : System.Windows.Controls.UserControl
{
    private const string ToolHistorySlug = "ssh_tunnel";
    private const string ToolDisplayName = "SSH Tunnel";
    private const string NoConfigurationOptionLabel = "Configurar manualmente";

    private enum SshTunnelWorkspaceMode { Execution, Configuration }

    private readonly ObservableCollection<SshTunnelEntity> _entities = new();
    private readonly ObservableCollection<SshTunnelSelectionOption> _configurationOptions = new();
    private readonly ISshTunnelFacade _facade;
    private SshTunnelEntity? _currentEntity;
    private SshTunnelWorkspaceMode _currentMode = SshTunnelWorkspaceMode.Execution;
    private CancellationTokenSource? _executionCts;
    private bool _isExecuting;
    private bool _initialized;
    private bool _suppressSelectionChanged;
    private bool _isConfigurationDraft;

    public SshTunnelWorkspaceView(ISshTunnelFacade facade)
    {
        _facade = facade;
        InitializeComponent();

        StrictHostKeyCheckingCombo.ItemsSource = Enum.GetValues<SshStrictHostKeyChecking>();
        StrictHostKeyCheckingCombo.SelectedIndex = 0;
        ConfigurationsCombo.ItemsSource = _configurationOptions;

        Loaded += View_Loaded;
        ApplyModeState();
        RefreshStateIndicator();
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
        SetMode(SshTunnelWorkspaceMode.Execution, "Modo execução ativado.");
    }

    public void ActivateConfigurationMode()
    {
        if (_isExecuting)
            return;

        SetMode(SshTunnelWorkspaceMode.Configuration, "Modo configuração ativado.");
        ResetConfigurationState();
    }

    // -- Navegação de modo -----------------------------------------------------

    private void SwitchToExecution_Click(object sender, RoutedEventArgs e) => SetMode(SshTunnelWorkspaceMode.Execution, "Modo execução ativado.");
    private void SwitchToConfiguration_Click(object sender, RoutedEventArgs e) => SetMode(SshTunnelWorkspaceMode.Configuration, "Modo configuração ativado.");

    private void SetMode(SshTunnelWorkspaceMode mode, string status)
    {
        if (_isExecuting) return;
        _currentMode = mode;
        ExecutionStatusText.Text = status;
        ApplyModeState();
    }

    // -- Entidades -------------------------------------------------------------

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
            _configurationOptions.Add(new SshTunnelSelectionOption(NoConfigurationOptionLabel, null));
            foreach (var item in _entities)
                _configurationOptions.Add(new SshTunnelSelectionOption(item.Name, item));
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

    private void SetSelectedOption(SshTunnelEntity? entity)
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
        if (ConfigurationsCombo.SelectedItem is not SshTunnelSelectionOption opt) return;
        if (opt.Entity is null)
        {
            CreateNewEntity();
            _isConfigurationDraft = false;
            ApplyModeState();
            return;
        }

        _currentEntity = opt.Entity;
        BindEntityToForm(_currentEntity);
        RefreshConfigSummary();
        ExecutionStatusText.Text = $"Configuração \"{_currentEntity.Name}\" carregada.";
        if (_currentMode == SshTunnelWorkspaceMode.Configuration)
            _isConfigurationDraft = true;
        ApplyModeState();
    }

    // -- CRUD -----------------------------------------------------------------

    private void ActionNew_Click(object sender, RoutedEventArgs e)
    {
        if (_isExecuting) return;

        if (_currentMode == SshTunnelWorkspaceMode.Execution)
        {
            SetMode(SshTunnelWorkspaceMode.Configuration, "Modo configuração ativado.");
            ResetConfigurationState();
            return;
        }

        _isConfigurationDraft = true;
        CreateNewEntity();
        SetMode(SshTunnelWorkspaceMode.Configuration, "Nova configuração.");
    }

    private async void ActionSave_Click(object sender, RoutedEventArgs e)
    {
        if (_isExecuting) return;

        if (_currentMode == SshTunnelWorkspaceMode.Execution)
        {
            await TunnelActionAsync(SshTunnelAction.Start).ConfigureAwait(true);
            return;
        }

        if (!_isConfigurationDraft)
        {
            ValidationUiService.ShowInline(ExecutionStatusText, "Clique em Novo para iniciar uma configuração.");
            return;
        }

        ReadFormIntoEntity();

        ValidationUiService.SetControlInvalid(NameInput, false);
        ValidationUiService.SetControlInvalid(SshHostInput, false);
        ValidationUiService.SetControlInvalid(SshUserInput, false);

        if (!ValidationUiService.ValidateRequiredFields(out var err,
            ValidationUiService.RequiredControl("Nome", NameInput, NameInput.Text),
            ValidationUiService.RequiredControl("Host SSH", SshHostInput, SshHostInput.Text),
            ValidationUiService.RequiredControl("Usuário SSH", SshUserInput, SshUserInput.Text)))
        {
            ValidationUiService.ShowInline(ExecutionStatusText, err);
            return;
        }

        var validation = await _facade.SaveAsync(_currentEntity!).ConfigureAwait(true);
        if (!validation.IsValid)
        {
            ValidationUiService.ShowInline(ExecutionStatusText, string.Join(" | ", validation.Errors.Select(x => x.Message)));
            return;
        }

        ValidationUiService.ClearInline(ExecutionStatusText);
        await ReloadEntitiesAsync().ConfigureAwait(true);
        ExecutionStatusText.Text = "Configuração salva.";
        ResetConfigurationState();
    }

    private async void ActionDelete_Click(object sender, RoutedEventArgs e)
    {
        if (_currentMode != SshTunnelWorkspaceMode.Configuration)
            return;

        if (_isExecuting || _currentEntity is null || string.IsNullOrWhiteSpace(_currentEntity.Id)) return;

        var confirm = Components.DevToolsMessageBox.Confirm(
            Window.GetWindow(this), $"Excluir \"{_currentEntity.Name}\"?", "Excluir");
        if (confirm != Components.DevToolsMessageBoxResult.Yes) return;

        await _facade.DeleteAsync(_currentEntity.Id).ConfigureAwait(true);
        _currentEntity = null;
        await ReloadEntitiesAsync().ConfigureAwait(true);
        ExecutionStatusText.Text = "Configuração excluída.";
    }

    private void ActionCancel_Click(object sender, RoutedEventArgs e)
    {
        if (_currentMode == SshTunnelWorkspaceMode.Configuration)
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
            mainWindow.OpenToolExecution("SshTunnel");
            return;
        }

        SetMode(SshTunnelWorkspaceMode.Execution, "Modo execução ativado.");
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

    // -- Execução (Start / Stop) -----------------------------------------------

    private async void Start_Click(object sender, RoutedEventArgs e) => await TunnelActionAsync(SshTunnelAction.Start);
    private async void Stop_Click(object sender, RoutedEventArgs e) => await TunnelActionAsync(SshTunnelAction.Stop);

    private async Task TunnelActionAsync(SshTunnelAction action)
    {
        if (_isExecuting) return;

        if (action == SshTunnelAction.Start && (_currentEntity is null || string.IsNullOrWhiteSpace(_currentEntity.SshHost)))
        {
            ValidationUiService.ShowInline(ExecutionStatusText, "Selecione ou configure um túnel antes de iniciar.");
            return;
        }

        var config = _currentEntity is not null ? TunnelConfiguration.FromEntity(_currentEntity) : null;
        var request = new SshTunnelRequest { Action = action, Configuration = config };
        await ToolHistoryViewHelper.RecordAsync(ToolHistorySlug, WorkspaceRoot, $"Executar SSH ({action})").ConfigureAwait(true);

        _executionCts?.Dispose();
        _executionCts = new CancellationTokenSource();
        _isExecuting = true;
        ApplyModeState();
        ExecutionStatusText.Text = action == SshTunnelAction.Start ? "Iniciando túnel SSH..." : "Encerrando túnel SSH...";

        try
        {
            var result = await _facade.ExecuteAsync(request, _executionCts.Token).ConfigureAwait(true);

            if (!result.IsSuccess)
            {
                ValidationUiService.ShowInline(ExecutionStatusText,
                    string.Join(" | ", result.Errors.Select(x => x.Message)));
            }
            else
            {
                ValidationUiService.ClearInline(ExecutionStatusText);
                ExecutionStatusText.Text = action == SshTunnelAction.Start
                    ? "Túnel SSH iniciado."
                    : "Túnel SSH encerrado.";
            }
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
            RefreshStateIndicator();
        }
    }

    // -- Estado visual do túnel ------------------------------------------------

    private void RefreshStateIndicator()
    {
        var state = _facade.CurrentState;
        var isOn  = _facade.IsRunning;

        SshStatusText.Text = state switch
        {
            TunnelState.On    => "● Túnel ativo",
            TunnelState.Error => "✕ Erro",
            _                 => "○ Desligado"
        };

        var bgKey = state switch
        {
            TunnelState.On    => "DevToolsBrushSuccess",
            TunnelState.Error => "DevToolsBrushError",
            _                 => "DevToolsBrushBorder"
        };

        SshStatusBadge.Background = TryFindResource(bgKey) is System.Windows.Media.Brush b ? b : System.Windows.Media.Brushes.Transparent;
        StartButton.IsEnabled  = !isOn && !_isExecuting;
        StopButton.IsEnabled   = isOn  && !_isExecuting;
        RefreshConfigSummary();
    }

    private void RefreshConfigSummary()
    {
        if (_currentEntity is null || string.IsNullOrWhiteSpace(_currentEntity.SshHost))
        {
            ConfigSummaryPanel.Visibility = Visibility.Collapsed;
            return;
        }

        var e = _currentEntity;
        ConfigSummaryPanel.Visibility = Visibility.Visible;
        ConfigSummaryText.Text =
            $"ssh -L {e.LocalBindHost}:{e.LocalPort}:{e.RemoteHost}:{e.RemotePort} " +
            $"{e.SshUser}@{e.SshHost}:{e.SshPort}\n" +
            $"{e.LocalBindHost}:{e.LocalPort} -> {e.RemoteHost}:{e.RemotePort} via {e.SshHost}";
    }

    // -- Binding ---------------------------------------------------------------

    private void BindEntityToForm(SshTunnelEntity entity)
    {
        _currentEntity = entity;
        NameInput.Text = entity.Name;
        SshHostInput.Text = entity.SshHost;
        SshPortInput.Text = entity.SshPort.ToString();
        SshUserInput.Text = entity.SshUser;
        LocalBindHostInput.Text = entity.LocalBindHost;
        LocalPortInput.Text = entity.LocalPort.ToString();
        RemoteHostInput.Text = entity.RemoteHost;
        RemotePortInput.Text = entity.RemotePort.ToString();
        IdentityFileSelector.SelectedPath = entity.IdentityFile ?? string.Empty;
        StrictHostKeyCheckingCombo.SelectedItem = entity.StrictHostKeyChecking;
        ConnectTimeoutInput.Text = entity.ConnectTimeoutSeconds?.ToString() ?? string.Empty;
        IsDefaultCheck.IsChecked = entity.IsDefault;
    }

    private void ReadFormIntoEntity()
    {
        if (_currentEntity is null) return;
        _currentEntity.Name = NameInput.Text.Trim();
        _currentEntity.SshHost = SshHostInput.Text.Trim();
        _currentEntity.SshPort = int.TryParse(SshPortInput.Text.Trim(), out var sp) ? sp : 22;
        _currentEntity.SshUser = SshUserInput.Text.Trim();
        _currentEntity.LocalBindHost = LocalBindHostInput.Text.Trim();
        _currentEntity.LocalPort = int.TryParse(LocalPortInput.Text.Trim(), out var lp) ? lp : 14331;
        _currentEntity.RemoteHost = RemoteHostInput.Text.Trim();
        _currentEntity.RemotePort = int.TryParse(RemotePortInput.Text.Trim(), out var rp) ? rp : 1433;
        _currentEntity.IdentityFile = string.IsNullOrWhiteSpace(IdentityFileSelector.SelectedPath) ? null : IdentityFileSelector.SelectedPath.Trim();
        _currentEntity.StrictHostKeyChecking = StrictHostKeyCheckingCombo.SelectedItem is SshStrictHostKeyChecking s ? s : SshStrictHostKeyChecking.Default;
        _currentEntity.ConnectTimeoutSeconds = int.TryParse(ConnectTimeoutInput.Text.Trim(), out var ct) ? ct : null;
        _currentEntity.IsDefault = IsDefaultCheck.IsChecked ?? false;
    }

    private void CreateNewEntity()
    {
        _currentEntity = new SshTunnelEntity
        {
            Name = "SSH Tunnel 1"
        };
        BindEntityToForm(_currentEntity);
        SetSelectedOption(null);
        RefreshConfigSummary();
        ApplyModeState();
    }

    private void ResetConfigurationState()
    {
        _isConfigurationDraft = false;
        _currentEntity = new SshTunnelEntity();
        SetSelectedOption(null);
        BindEntityToForm(_currentEntity);
        RefreshConfigSummary();
        ValidationUiService.ClearInline(ExecutionStatusText);
        ApplyModeState();
    }

    private void ApplyModeState()
    {
        var inConfiguration = _currentMode == SshTunnelWorkspaceMode.Configuration;
        var inExecution = _currentMode == SshTunnelWorkspaceMode.Execution;
        var hasSelected = _currentEntity is not null;

        ConfigurationModeHint.Visibility = Visibility.Collapsed;
        ConfigurationMetadataSection.Visibility = inConfiguration ? Visibility.Visible : Visibility.Collapsed;

        WorkspaceTitleText.Text = inConfiguration ? "SSH Tunnel - Configuração" : "SSH Tunnel";
        WorkspaceSubtitleText.Text = inConfiguration
            ? "Salve os parâmetros de conexão e mapeamento para reutilizar."
            : "Cria e gerencia túneis SSH para redirecionamento de portas.";

        Actions.NewText = "Novo";
        Actions.SaveText = inConfiguration ? "Salvar" : "Executar";
        Actions.SaveIconKind = inConfiguration ? "ContentSave" : "Play";
        Actions.CancelText = "Cancelar";
        Actions.GoToToolText = "Ir para ferramenta";
        Actions.BackText = _isExecuting ? "Cancelar" : "Voltar";
        Actions.BackIconKind = _isExecuting ? "CloseCircleOutline" : "ArrowLeft";

        Actions.ShowHelp = true;
        Actions.ShowHistory = inExecution;
        Actions.HelpContextKey = inConfiguration ? "sshtunnel:configuration" : "sshtunnel:execution";
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
        Actions.Visibility = Visibility.Visible;

        StartButton.IsEnabled = !_isExecuting && !_facade.IsRunning;
        StopButton.IsEnabled  = !_isExecuting && _facade.IsRunning;
    }
}

