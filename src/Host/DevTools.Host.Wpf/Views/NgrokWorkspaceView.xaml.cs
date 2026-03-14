using System.Collections.ObjectModel;
using System.Windows;
using DevTools.Host.Wpf.Facades;
using DevTools.Host.Wpf.Services;
using DevTools.Ngrok.Models;

namespace DevTools.Host.Wpf.Views;

public partial class NgrokWorkspaceView : System.Windows.Controls.UserControl
{
    private const string NoConfigurationOptionLabel = "Configurar manualmente";

    private enum NgrokWorkspaceMode { Execution, Configuration }

    private readonly ObservableCollection<NgrokEntity> _entities = new();
    private readonly ObservableCollection<NgrokSelectionOption> _configurationOptions = new();
    private readonly INgrokFacade _facade;
    private NgrokEntity? _currentEntity;
    private NgrokWorkspaceMode _currentMode = NgrokWorkspaceMode.Execution;
    private bool _initialized;
    private bool _suppressSelectionChanged;
    private bool _isConfigurationDraft;

    public NgrokWorkspaceView(INgrokFacade facade)
    {
        _facade = facade;
        InitializeComponent();

        ProtocolCombo.ItemsSource = new[] { "http", "https" };
        ProtocolCombo.SelectedIndex = 0;

        ConfigurationsCombo.ItemsSource = _configurationOptions;
        Loaded += View_Loaded;
        ApplyModeState();
    }

    private async void View_Loaded(object sender, RoutedEventArgs e)
    {
        if (_initialized) return;
        _initialized = true;
        await ReloadEntitiesAsync().ConfigureAwait(true);
    }

    public void ActivateExecutionMode()
    {
        if (_currentEntity is null)
            CreateNewEntity();

        _isConfigurationDraft = false;
        SetMode(NgrokWorkspaceMode.Execution, "Modo execucao ativado.");
    }

    public void ActivateConfigurationMode()
    {
        SetMode(NgrokWorkspaceMode.Configuration, "Modo configuracao ativado.");
        ResetConfigurationState();
    }

    // â”€â”€ NavegaÃ§Ã£o de modo â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private void SwitchToExecution_Click(object sender, RoutedEventArgs e) => SetMode(NgrokWorkspaceMode.Execution, "Modo execuÃ§Ã£o ativado.");
    private void SwitchToConfiguration_Click(object sender, RoutedEventArgs e) => SetMode(NgrokWorkspaceMode.Configuration, "Modo configuraÃ§Ã£o ativado.");

    private void SetMode(NgrokWorkspaceMode mode, string status)
    {
        _currentMode = mode;
        ExecutionStatusText.Text = status;
        ApplyModeState();
    }

    // â”€â”€ Entidades â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

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
            _configurationOptions.Add(new NgrokSelectionOption(NoConfigurationOptionLabel, null));
            foreach (var item in _entities)
                _configurationOptions.Add(new NgrokSelectionOption(item.Name, item));
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

    private void SetSelectedOption(NgrokEntity? entity)
    {
        _suppressSelectionChanged = true;
        ConfigurationsCombo.SelectedItem = entity is null
            ? null
            : _configurationOptions.FirstOrDefault(o => o.Entity?.Id == entity.Id);
        _suppressSelectionChanged = false;
    }

    private async void ConfigurationsCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_suppressSelectionChanged) return;
        if (ConfigurationsCombo.SelectedItem is not NgrokSelectionOption opt) return;
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
        if (_currentMode == NgrokWorkspaceMode.Configuration)
            _isConfigurationDraft = true;
        ApplyModeState();
    }

    // â”€â”€ CRUD â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private void ActionNew_Click(object sender, RoutedEventArgs e)
    {
        if (_currentMode == NgrokWorkspaceMode.Execution)
        {
            SetMode(NgrokWorkspaceMode.Configuration, "Modo configuracao ativado.");
            ResetConfigurationState();
            return;
        }

        _isConfigurationDraft = true;
        CreateNewEntity();
        SetMode(NgrokWorkspaceMode.Configuration, "Nova configuracao.");
    }

    private async void ActionSave_Click(object sender, RoutedEventArgs e)
    {
        if (_currentMode == NgrokWorkspaceMode.Execution)
        {
            await StartHttpFromActionBarAsync().ConfigureAwait(true);
            return;
        }

        if (!_isConfigurationDraft)
        {
            ValidationUiService.ShowInline(ExecutionStatusText, "Clique em Novo para iniciar uma configuração.");
            return;
        }

        ReadFormIntoEntity();

        ValidationUiService.SetControlInvalid(NameInput, false);
        if (!ValidationUiService.ValidateRequiredFields(out var err,
            ValidationUiService.RequiredControl("Nome", NameInput, NameInput.Text)))
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
        ExecutionStatusText.Text = "Configuracao salva.";
        ResetConfigurationState();
    }

    private async void ActionDelete_Click(object sender, RoutedEventArgs e)
    {
        if (_currentMode != NgrokWorkspaceMode.Configuration)
            return;

        if (_currentEntity is null || string.IsNullOrWhiteSpace(_currentEntity.Id)) return;

        var confirm = Components.DevToolsMessageBox.Confirm(
            Window.GetWindow(this), $"Excluir \"{_currentEntity.Name}\"?", "Excluir");
        if (confirm != Components.DevToolsMessageBoxResult.Yes) return;

        await _facade.DeleteAsync(_currentEntity.Id).ConfigureAwait(true);
        _currentEntity = null;
        await ReloadEntitiesAsync().ConfigureAwait(true);
        ExecutionStatusText.Text = "Configuracao excluida.";
    }

    // â”€â”€ AÃ§Ãµes de execuÃ§Ã£o â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private void ActionCancel_Click(object sender, RoutedEventArgs e)
    {
        if (_currentMode != NgrokWorkspaceMode.Configuration)
        {
            ActionBack_Click(sender, e);
            return;
        }

        ResetConfigurationState();
        ExecutionStatusText.Text = "Configuração cancelada.";
    }

    private void ActionGoToTool_Click(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is MainWindow mainWindow)
        {
            mainWindow.OpenToolExecution("Ngrok");
            return;
        }

        SetMode(NgrokWorkspaceMode.Execution, "Modo execucao ativado.");
    }

    private void ActionBack_Click(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is MainWindow mainWindow)
            mainWindow.OpenFerramentasHome();
    }

    private async void ListTunnels_Click(object sender, RoutedEventArgs e)
        => await RunActionAsync(new NgrokRequest { Action = NgrokAction.ListTunnels, BaseUrl = GetBaseUrl() });

    private async void Status_Click(object sender, RoutedEventArgs e)
        => await RunActionAsync(new NgrokRequest { Action = NgrokAction.Status, BaseUrl = GetBaseUrl() });

    private async void KillAll_Click(object sender, RoutedEventArgs e)
    {
        var confirm = Components.DevToolsMessageBox.Confirm(
            Window.GetWindow(this), "Encerrar todos os processos ngrok?", "Kill all");
        if (confirm != Components.DevToolsMessageBoxResult.Yes) return;
        await RunActionAsync(new NgrokRequest { Action = NgrokAction.KillAll, BaseUrl = GetBaseUrl() });
    }

    private async void StartHttp_Click(object sender, RoutedEventArgs e)
        => await StartHttpFromActionBarAsync().ConfigureAwait(true);

    private async Task StartHttpFromActionBarAsync()
    {
        ValidationUiService.SetControlInvalid(StartPortInput, false);
        if (!int.TryParse(StartPortInput.Text.Trim(), out var port) || port <= 0)
        {
            ValidationUiService.SetControlInvalid(StartPortInput, true);
            ValidationUiService.ShowInline(ExecutionStatusText, "Porta inválida.");
            return;
        }

        var execPath = _currentEntity?.ExecutablePath;
        var protocol = ProtocolCombo.SelectedItem as string ?? "http";

        await RunActionAsync(new NgrokRequest
        {
            Action = NgrokAction.StartHttp,
            BaseUrl = GetBaseUrl(),
            StartOptions = new NgrokStartOptions(protocol, port, execPath)
        }).ConfigureAwait(true);
    }

    private async Task RunActionAsync(NgrokRequest request)
    {
        ExecutionStatusText.Text = "Executando...";
        

        try
        {
            var result = await _facade.ExecuteAsync(request).ConfigureAwait(true);

            if (!result.IsSuccess)
            {
                ValidationUiService.ShowInline(ExecutionStatusText,
                    string.Join(" | ", result.Errors.Select(x => x.Message)));
                return;
            }

            ValidationUiService.ClearInline(ExecutionStatusText);
            var data = result.Value!;

            switch (data.Action)
            {
                case NgrokAction.ListTunnels:
                    var tunnels = data.Tunnels ?? [];

                    TunnelListPanel.Visibility = Visibility.Visible;
                    TunnelList.ItemsSource = tunnels;
                    TunnelListHeader.Text = tunnels.Count == 0 ? "Tuneis ativos" : $"Tuneis ativos ({tunnels.Count})";
                    EmptyStateText.Visibility = tunnels.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

                    ExecutionStatusText.Text = $"Listados {tunnels.Count} tÃºnel(is).";
                    break;

                case NgrokAction.StartHttp:
                    ExecutionStatusText.Text = data.ProcessId.HasValue
                        ? $"Ngrok iniciado. PID: {data.ProcessId}"
                        : "Ngrok iniciado.";
                    break;

                case NgrokAction.KillAll:
                    ExecutionStatusText.Text = $"{data.Killed ?? 0} processo(s) encerrado(s).";
                    break;

                case NgrokAction.Status:
                    ExecutionStatusText.Text = (data.HasAny ?? false)
                        ? "Ngrok estÃ¡ em execuÃ§Ã£o."
                        : "Nenhum processo ngrok em execuÃ§Ã£o.";
                    break;

                case NgrokAction.CloseTunnel:
                    ExecutionStatusText.Text = (data.Closed ?? false) ? "TÃºnel fechado." : "TÃºnel nÃ£o encontrado.";
                    break;

                default:
                    ExecutionStatusText.Text = "OperaÃ§Ã£o concluÃ­da.";
                    break;
            }
        }
        catch (Exception ex)
        {
            ValidationUiService.ShowInline(ExecutionStatusText, ex.Message);
        }
    }

    private void CopyUrl_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button btn)
            return;

        var url = btn.Tag as string;
        if (string.IsNullOrWhiteSpace(url))
            return;

        try
        {
            System.Windows.Clipboard.SetText(url);
            ExecutionStatusText.Text = "URL copiada para a Ã¡rea de transferÃªncia.";
        }
        catch (Exception ex)
        {
            ValidationUiService.ShowInline(ExecutionStatusText, $"Falha ao copiar URL: {ex.Message}");
        }
    }

    // â”€â”€ Binding â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private string GetBaseUrl() =>
        _currentEntity?.BaseUrl?.Trim() is { Length: > 0 } url
            ? url
            : "http://127.0.0.1:4040/";

    private void BindEntityToForm(NgrokEntity entity)
    {
        _currentEntity = entity;
        NameInput.Text = entity.Name;
        AuthTokenInput.Text = entity.AuthToken;
        ExecutablePathSelector.SelectedPath = entity.ExecutablePath;
        AdditionalArgsInput.Text = entity.AdditionalArgs;
        BaseUrlInput.Text = entity.BaseUrl ?? "http://127.0.0.1:4040/";
        IsDefaultCheck.IsChecked = entity.IsDefault;
    }

    private void ReadFormIntoEntity()
    {
        if (_currentEntity is null) return;
        _currentEntity.Name = NameInput.Text.Trim();
        _currentEntity.AuthToken = AuthTokenInput.Text.Trim();
        _currentEntity.ExecutablePath = ExecutablePathSelector.SelectedPath?.Trim() ?? string.Empty;
        _currentEntity.AdditionalArgs = AdditionalArgsInput.Text.Trim();
        _currentEntity.BaseUrl = BaseUrlInput.Text.Trim();
        _currentEntity.IsDefault = IsDefaultCheck.IsChecked ?? false;
    }

    private void CreateNewEntity()
    {
        _currentEntity = new NgrokEntity();
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

    private void ApplyModeState()
    {
        var inConfiguration = _currentMode == NgrokWorkspaceMode.Configuration;
        var inExecution = _currentMode == NgrokWorkspaceMode.Execution;
        var hasSelected = _currentEntity is not null;

        ConfigurationModeHint.Visibility = inConfiguration ? Visibility.Visible : Visibility.Collapsed;
        ConfigurationMetadataSection.Visibility = inConfiguration ? Visibility.Visible : Visibility.Collapsed;

        WorkspaceTitleText.Text = inConfiguration ? "Ngrok - Configuracao" : "Ngrok";
        WorkspaceSubtitleText.Text = inConfiguration
            ? "Salve uma configuracao com token e caminhos para reutilizar."
            : "Gerencia tuneis HTTP/HTTPS via ngrok. Inicie, liste e copie URLs publicas.";

        Actions.NewText = "Novo";
        Actions.SaveText = inConfiguration ? "Salvar" : "Executar";
        Actions.SaveIconKind = inConfiguration ? "ContentSave" : "Play";
        Actions.CancelText = "Cancelar";
        Actions.GoToToolText = "Ir para ferramenta";
        Actions.BackText = "Voltar";
        Actions.BackIconKind = "ArrowLeft";

        Actions.ShowHelp = true;
        Actions.ShowNew = inConfiguration;
        Actions.ShowSave = inConfiguration || inExecution;
        Actions.ShowDelete = false;
        Actions.ShowCancel = inConfiguration;
        Actions.ShowGoToTool = inConfiguration;
        Actions.ShowBack = inExecution;
        Actions.Visibility = Visibility.Visible;

        Actions.CanHelp = true;
        Actions.CanNew = inConfiguration;
        Actions.CanSave = inExecution ? hasSelected : _isConfigurationDraft;
        Actions.CanDelete = false;
        Actions.CanCancel = inConfiguration && _isConfigurationDraft;
        Actions.CanGoToTool = inConfiguration;
        Actions.CanBack = inExecution;
    }
}


