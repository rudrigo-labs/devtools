using System.Collections.ObjectModel;
using System.Collections.Generic;
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
        SetMode(SshTunnelWorkspaceMode.Execution, "Modo execuÃ§Ã£o ativado.");
    }

    public void ActivateConfigurationMode()
    {
        if (_isExecuting)
            return;

        SetMode(SshTunnelWorkspaceMode.Configuration, "Modo configuraÃ§Ã£o ativado.");
        if (_currentEntity is null)
            CreateNewEntity();

        _isConfigurationDraft = true;
        BindEntityToForm(_currentEntity!);
        RefreshConfigSummary();
        ClearInlineValidationStates();
        ValidationUiService.ClearInline(ExecutionStatusText);
        ApplyModeState();
    }

    // -- NavegaÃ§Ã£o de modo -----------------------------------------------------

    private void SwitchToExecution_Click(object sender, RoutedEventArgs e) => SetMode(SshTunnelWorkspaceMode.Execution, "Modo execuÃ§Ã£o ativado.");
    private void SwitchToConfiguration_Click(object sender, RoutedEventArgs e) => ActivateConfigurationMode();

    private void SetMode(SshTunnelWorkspaceMode mode, string status)
    {
        if (_isExecuting) return;
        _currentMode = mode;
        ValidationUiService.ClearInline(ExecutionStatusText);
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
        ExecutionStatusText.Text = $"ConfiguraÃ§Ã£o \"{_currentEntity.Name}\" carregada.";
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
            SetMode(SshTunnelWorkspaceMode.Configuration, "Modo configuraÃ§Ã£o ativado.");
            ResetConfigurationState();
            return;
        }

        _isConfigurationDraft = true;
        CreateNewEntity();
        SetMode(SshTunnelWorkspaceMode.Configuration, "Nova configuraÃ§Ã£o.");
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
            ValidationUiService.ShowInline(ExecutionStatusText, "Clique em Novo para iniciar uma configuraÃ§Ã£o.");
            return;
        }
        if (!TryValidateCurrentForm(requireName: true))
            return;

        var validation = await _facade.SaveAsync(_currentEntity!).ConfigureAwait(true);
        if (!validation.IsValid)
        {
            ValidationUiService.ShowInline(ExecutionStatusText, string.Join(" | ", validation.Errors.Select(x => x.Message)));
            return;
        }

        ValidationUiService.ClearInline(ExecutionStatusText);
        await ReloadEntitiesAsync().ConfigureAwait(true);
        ExecutionStatusText.Text = "ConfiguraÃ§Ã£o salva.";
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
        ExecutionStatusText.Text = "ConfiguraÃ§Ã£o excluÃ­da.";
    }

    private async void ActionCancel_Click(object sender, RoutedEventArgs e)
    {
        if (_currentMode == SshTunnelWorkspaceMode.Configuration)
        {
            ResetConfigurationState();
            ExecutionStatusText.Text = "Opera\u00E7\u00E3o cancelada.";
            return;
        }

        await TunnelActionAsync(SshTunnelAction.Stop).ConfigureAwait(true);
    }

    private void ActionGoToTool_Click(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is MainWindow mainWindow)
        {
            mainWindow.OpenToolExecution("SshTunnel");
            return;
        }

        SetMode(SshTunnelWorkspaceMode.Execution, "Modo execuÃ§Ã£o ativado.");
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

    // -- ExecuÃ§Ã£o (Start / Stop) -----------------------------------------------

    private async Task TunnelActionAsync(SshTunnelAction action)
    {
        if (_isExecuting) return;

        if (action == SshTunnelAction.Start && !TryValidateCurrentForm(requireName: false))
            return;

        var config = _currentEntity is not null ? TunnelConfiguration.FromEntity(_currentEntity) : null;
        if (action == SshTunnelAction.Stop && config is not null && string.IsNullOrWhiteSpace(config.Name))
            config = null;

        var request = new SshTunnelRequest { Action = action, Configuration = config };
        await ToolHistoryViewHelper.RecordAsync(ToolHistorySlug, WorkspaceRoot, $"Executar SSH ({action})").ConfigureAwait(true);

        _executionCts?.Dispose();
        _executionCts = new CancellationTokenSource();
        _isExecuting = true;
        ApplyModeState();
        ExecutionStatusText.Text = action == SshTunnelAction.Start ? "Iniciando t\u00FAnel SSH..." : "Encerrando t\u00FAnel SSH...";

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
                    ? "T\u00FAnel SSH iniciado."
                    : "T\u00FAnel SSH encerrado.";

                if (action == SshTunnelAction.Start && Window.GetWindow(this) is MainWindow mainWindow)
                    await mainWindow.MoveToBackgroundTrayAsync().ConfigureAwait(true);
            }
        }
        catch (OperationCanceledException)
        {
            ValidationUiService.ClearInline(ExecutionStatusText);
            ExecutionStatusText.Text = "Opera\u00E7\u00E3o cancelada.";
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

    // -- Estado visual do tÃºnel ------------------------------------------------

    private void RefreshStateIndicator()
    {
        var activeCount = _facade.ActiveTunnels.Count;
        var state = activeCount > 0 ? TunnelState.On : _facade.CurrentState;

        SshStatusText.Text = state switch
        {
            TunnelState.On    => activeCount == 1
                ? "\u25CF 1 t\u00FAnel ativo"
                : $"\u25CF {activeCount} t\u00FAneis ativos",
            TunnelState.Error => "\u2715 Erro",
            _                 => "\u25CB Desligado"
        };

        var bgKey = state switch
        {
            TunnelState.On    => "DevToolsBrushSuccess",
            TunnelState.Error => "DevToolsBrushError",
            _                 => "DevToolsBrushBorder"
        };

        SshStatusBadge.Background = TryFindResource(bgKey) is System.Windows.Media.Brush b ? b : System.Windows.Media.Brushes.Transparent;
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
        _currentEntity.SshPort = ParseIntOrZero(SshPortInput.Text);
        _currentEntity.SshUser = SshUserInput.Text.Trim();
        _currentEntity.LocalBindHost = LocalBindHostInput.Text.Trim();
        _currentEntity.LocalPort = ParseIntOrZero(LocalPortInput.Text);
        _currentEntity.RemoteHost = RemoteHostInput.Text.Trim();
        _currentEntity.RemotePort = ParseIntOrZero(RemotePortInput.Text);
        _currentEntity.IdentityFile = string.IsNullOrWhiteSpace(IdentityFileSelector.SelectedPath) ? null : IdentityFileSelector.SelectedPath.Trim();
        _currentEntity.StrictHostKeyChecking = StrictHostKeyCheckingCombo.SelectedItem is SshStrictHostKeyChecking s ? s : SshStrictHostKeyChecking.Default;
        _currentEntity.ConnectTimeoutSeconds = ParseOptionalInt(ConnectTimeoutInput.Text);
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
        ClearInlineValidationStates();
        RefreshConfigSummary();
        ValidationUiService.ClearInline(ExecutionStatusText);
        ApplyModeState();
    }


    private void ClearInlineValidationStates()
    {
        ValidationUiService.SetControlInvalid(NameInput, false);
        ValidationUiService.SetControlInvalid(SshHostInput, false);
        ValidationUiService.SetControlInvalid(SshPortInput, false);
        ValidationUiService.SetControlInvalid(SshUserInput, false);
        ValidationUiService.SetControlInvalid(LocalBindHostInput, false);
        ValidationUiService.SetControlInvalid(LocalPortInput, false);
        ValidationUiService.SetControlInvalid(RemoteHostInput, false);
        ValidationUiService.SetControlInvalid(RemotePortInput, false);
        ValidationUiService.SetControlInvalid(ConnectTimeoutInput, false);
    }
    private bool TryValidateCurrentForm(bool requireName)
    {
        _currentEntity ??= new SshTunnelEntity();
        ReadFormIntoEntity();
        ClearInlineValidationStates();

        var requiredFields = new List<ValidationUiService.RequiredField>();
        if (requireName)
            requiredFields.Add(ValidationUiService.RequiredControl("Nome", NameInput, NameInput.Text));

        requiredFields.Add(ValidationUiService.RequiredControl("Host SSH", SshHostInput, SshHostInput.Text));
        requiredFields.Add(ValidationUiService.RequiredControl("UsuÃ¡rio SSH", SshUserInput, SshUserInput.Text));
        requiredFields.Add(ValidationUiService.RequiredControl("Bind local", LocalBindHostInput, LocalBindHostInput.Text));
        requiredFields.Add(ValidationUiService.RequiredControl("Host remoto", RemoteHostInput, RemoteHostInput.Text));

        if (!ValidationUiService.ValidateRequiredFields(out var requiredError, requiredFields.ToArray()))
        {
            ValidationUiService.ShowInline(ExecutionStatusText, requiredError);
            return false;
        }

        if (!TryValidatePort(SshPortInput, "Porta SSH", out var sshPort))
            return false;
        if (!TryValidatePort(LocalPortInput, "Porta local", out var localPort))
            return false;
        if (!TryValidatePort(RemotePortInput, "Porta remota", out var remotePort))
            return false;
        if (!TryValidateOptionalPositiveInt(ConnectTimeoutInput, "Timeout de conexÃ£o (s)", out var timeout))
            return false;

        _currentEntity.SshPort = sshPort;
        _currentEntity.LocalPort = localPort;
        _currentEntity.RemotePort = remotePort;
        _currentEntity.ConnectTimeoutSeconds = timeout;
        return true;
    }

    private bool TryValidatePort(System.Windows.Controls.TextBox input, string fieldLabel, out int value)
    {
        value = 0;
        if (input is null)
            return false;

        var raw = input.Text.Trim();
        if (!int.TryParse(raw, out value) || value < 1 || value > 65535)
        {
            ValidationUiService.SetControlInvalid(input, true, tooltipMessage: $"{fieldLabel} deve estar entre 1 e 65535.");
            ValidationUiService.ShowInline(ExecutionStatusText, $"{fieldLabel} deve estar entre 1 e 65535.");
            return false;
        }

        ValidationUiService.SetControlInvalid(input, false);
        return true;
    }

    private bool TryValidateOptionalPositiveInt(System.Windows.Controls.TextBox input, string fieldLabel, out int? value)
    {
        value = null;
        if (input is null)
            return true;

        var raw = input.Text.Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            ValidationUiService.SetControlInvalid(input, false);
            return true;
        }

        if (!int.TryParse(raw, out var parsed) || parsed <= 0)
        {
            ValidationUiService.SetControlInvalid(input, true, tooltipMessage: $"{fieldLabel} deve ser um n\u00FAmero inteiro maior que zero.");
            ValidationUiService.ShowInline(ExecutionStatusText, $"{fieldLabel} deve ser um n\u00FAmero inteiro maior que zero.");
            return false;
        }

        value = parsed;
        ValidationUiService.SetControlInvalid(input, false);
        return true;
    }

    private static int ParseIntOrZero(string? raw) =>
        int.TryParse(raw?.Trim(), out var parsed) ? parsed : 0;

    private static int? ParseOptionalInt(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        return int.TryParse(raw.Trim(), out var parsed) ? parsed : 0;
    }
    private void ApplyModeState()
    {
        var inConfiguration = _currentMode == SshTunnelWorkspaceMode.Configuration;
        var inExecution = _currentMode == SshTunnelWorkspaceMode.Execution;
        var hasSelected = _currentEntity is not null;
        var currentTunnelRunning = IsCurrentTunnelRunning();

        ConfigurationModeHint.Visibility = Visibility.Collapsed;
        ConfigurationMetadataSection.Visibility = inConfiguration ? Visibility.Visible : Visibility.Collapsed;

        WorkspaceTitleText.Text = inConfiguration ? "SSH Tunnel - ConfiguraÃ§Ã£o" : "SSH Tunnel";
        WorkspaceSubtitleText.Text = inConfiguration
            ? "Salve os parÃ¢metros de conexÃ£o e mapeamento para reutilizar."
            : "Cria e gerencia tÃºneis SSH para redirecionamento de portas.";

        Actions.NewText = "Novo";
        Actions.SaveText = inConfiguration ? "Salvar" : "Executar";
        Actions.SaveIconKind = inConfiguration ? "ContentSave" : "Play";
        Actions.CancelText = inConfiguration ? "Cancelar" : "Parar t\u00FAnel";
        Actions.GoToToolText = "Ir para ferramenta";
        Actions.BackText = _isExecuting ? "Cancelar" : "Voltar";
        Actions.BackIconKind = _isExecuting ? "CloseCircleOutline" : "ArrowLeft";

        Actions.ShowHelp = true;
        Actions.ShowHistory = inExecution;
        Actions.HelpContextKey = inConfiguration ? "sshtunnel:configuration" : "sshtunnel:execution";
        Actions.ShowNew = inConfiguration;
        Actions.ShowSave = inConfiguration || inExecution;
        Actions.ShowDelete = false;
        Actions.ShowCancel = inConfiguration || inExecution;
        Actions.ShowGoToTool = false;
        Actions.ShowBack = inExecution;

        Actions.CanHelp = true;
        Actions.CanNew = inConfiguration && !_isExecuting && !_isConfigurationDraft;
        Actions.CanSave = !_isExecuting && (inExecution ? hasSelected : _isConfigurationDraft);
        Actions.CanDelete = false;
        Actions.CanCancel = !_isExecuting && (inConfiguration ? _isConfigurationDraft : currentTunnelRunning);
        Actions.CanGoToTool = false;
        Actions.CanBack = inExecution;
        Actions.Visibility = Visibility.Visible;
    }

    private bool IsCurrentTunnelRunning()
    {
        if (_currentEntity is null || string.IsNullOrWhiteSpace(_currentEntity.Name))
            return false;

        var name = _currentEntity.Name.Trim();
        return _facade.ActiveTunnels.Any(x =>
            string.Equals(x.Configuration.Name, name, StringComparison.OrdinalIgnoreCase));
    }
}

