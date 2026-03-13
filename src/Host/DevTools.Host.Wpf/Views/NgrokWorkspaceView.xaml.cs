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

        SetMode(NgrokWorkspaceMode.Execution, "Modo execucao ativado.");
    }

    public void ActivateConfigurationMode()
    {
        if (_currentEntity is null)
            CreateNewEntity();
        else
            BindEntityToForm(_currentEntity);

        SetMode(NgrokWorkspaceMode.Configuration, "Modo configuracao ativado.");
    }

    // ── Navegação de modo ─────────────────────────────────────────────────────

    private void SwitchToExecution_Click(object sender, RoutedEventArgs e) => SetMode(NgrokWorkspaceMode.Execution, "Modo execução ativado.");
    private void SwitchToConfiguration_Click(object sender, RoutedEventArgs e) => SetMode(NgrokWorkspaceMode.Configuration, "Modo configuração ativado.");

    private void SetMode(NgrokWorkspaceMode mode, string status)
    {
        _currentMode = mode;
        ExecutionStatusText.Text = status;
        ApplyModeState();
    }

    // ── Entidades ─────────────────────────────────────────────────────────────

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
        if (opt.Entity is null) { CreateNewEntity(); return; }
        _currentEntity = opt.Entity;
        BindEntityToForm(_currentEntity);
        ExecutionStatusText.Text = $"Configuração \"{_currentEntity.Name}\" carregada.";
        ApplyModeState();
    }

    // ── CRUD ─────────────────────────────────────────────────────────────────

    private void ActionNew_Click(object sender, RoutedEventArgs e)
    {
        if (_currentMode == NgrokWorkspaceMode.Execution)
        {
            if (_currentEntity is null)
                CreateNewEntity();

            SetMode(NgrokWorkspaceMode.Configuration, "Modo configuracao ativado.");
            return;
        }

        CreateNewEntity();
        SetMode(NgrokWorkspaceMode.Configuration, "Nova configuracao.");
    }

    private async void ActionSave_Click(object sender, RoutedEventArgs e)
    {
        if (_currentMode == NgrokWorkspaceMode.Execution) return;

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
        SetMode(NgrokWorkspaceMode.Execution, "Modo execucao ativado.");
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

    // ── Ações de execução ─────────────────────────────────────────────────────

    private void ActionCancel_Click(object sender, RoutedEventArgs e)
    {
        if (_currentMode != NgrokWorkspaceMode.Configuration)
            return;

        if (_currentEntity is not null)
            BindEntityToForm(_currentEntity);

        SetMode(NgrokWorkspaceMode.Execution, "Modo execucao ativado.");
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
        });
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

                    ExecutionStatusText.Text = $"Listados {tunnels.Count} túnel(is).";
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
                        ? "Ngrok está em execução."
                        : "Nenhum processo ngrok em execução.";
                    break;

                case NgrokAction.CloseTunnel:
                    ExecutionStatusText.Text = (data.Closed ?? false) ? "Túnel fechado." : "Túnel não encontrado.";
                    break;

                default:
                    ExecutionStatusText.Text = "Operação concluída.";
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
            ExecutionStatusText.Text = "URL copiada para a área de transferência.";
        }
        catch (Exception ex)
        {
            ValidationUiService.ShowInline(ExecutionStatusText, $"Falha ao copiar URL: {ex.Message}");
        }
    }

    // ── Binding ───────────────────────────────────────────────────────────────

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

    private void ApplyModeState()
    {
        var inConfiguration = _currentMode == NgrokWorkspaceMode.Configuration;
        var hasPersistedConfiguration = _currentEntity is not null && !string.IsNullOrWhiteSpace(_currentEntity.Id);

        ConfigurationModeHint.Visibility = inConfiguration ? Visibility.Visible : Visibility.Collapsed;
        ConfigurationMetadataSection.Visibility = inConfiguration ? Visibility.Visible : Visibility.Collapsed;

        WorkspaceTitleText.Text = inConfiguration ? "Ngrok - Configuracao" : "Ngrok";
        WorkspaceSubtitleText.Text = inConfiguration
            ? "Salve uma configuracao com token e caminhos para reutilizar."
            : "Gerencia tuneis HTTP/HTTPS via ngrok. Inicie, liste e copie URLs publicas.";

        Actions.NewText = inConfiguration ? "Novo" : "Configurar";
        Actions.SaveText = "Salvar";
        Actions.DeleteText = "Excluir";
        Actions.CancelText = "Voltar";

        Actions.ShowNew = true;
        Actions.ShowSave = inConfiguration;
        Actions.ShowDelete = inConfiguration;
        Actions.ShowCancel = inConfiguration;
        Actions.Visibility = inConfiguration ? Visibility.Visible : Visibility.Collapsed;

        Actions.CanNew = true;
        Actions.CanSave = inConfiguration;
        Actions.CanDelete = inConfiguration && hasPersistedConfiguration;
        Actions.CanCancel = inConfiguration;
    }
}

