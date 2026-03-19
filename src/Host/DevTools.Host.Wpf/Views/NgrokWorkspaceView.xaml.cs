using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Input;
using DevTools.Host.Wpf.Components;
using DevTools.Host.Wpf.Facades;
using DevTools.Host.Wpf.Services;
using DevTools.Ngrok.Models;

namespace DevTools.Host.Wpf.Views;

public partial class NgrokWorkspaceView : System.Windows.Controls.UserControl
{
    private const string ToolHistorySlug = "ngrok";
    private const string ToolDisplayName = "Ngrok";

    private enum NgrokWorkspaceMode { Execution, Configuration }

    private readonly ObservableCollection<NgrokEntity> _entities = new();
    private readonly INgrokFacade _facade;
    private readonly DependencyPropertyDescriptor? _pathDescriptor;
    private NgrokEntity? _currentEntity;
    private NgrokEntity? _baselineEntity;
    private NgrokWorkspaceMode _currentMode = NgrokWorkspaceMode.Execution;
    private bool _initialized;
    private bool _suppressGridSelection;
    private bool _suppressFormTracking;
    private bool _isCreatingNew;
    private bool _isDirty;
    private bool _useCurrentEntityOnNextExecution;
    private int _activeTunnelCount;

    public NgrokWorkspaceView(INgrokFacade facade)
    {
        _facade = facade;
        InitializeComponent();

        ConfigurationsGrid.ItemsSource = _entities;
        ProtocolCombo.ItemsSource = new[] { "http", "https" };
        ProtocolCombo.SelectedIndex = 0;

        _pathDescriptor = DependencyPropertyDescriptor.FromProperty(
            PathSelector.SelectedPathProperty,
            typeof(PathSelector));
        _pathDescriptor?.AddValueChanged(ExecutablePathSelector, ConfigurationField_Changed);

        Loaded += View_Loaded;
        Unloaded += View_Unloaded;
        ApplyModeState();
    }

    private void View_Unloaded(object sender, RoutedEventArgs e)
    {
        _pathDescriptor?.RemoveValueChanged(ExecutablePathSelector, ConfigurationField_Changed);
        Unloaded -= View_Unloaded;
    }

    private async void View_Loaded(object sender, RoutedEventArgs e)
    {
        if (_initialized)
            return;

        _initialized = true;
        await ReloadEntitiesAsync().ConfigureAwait(true);
        EnsureExecutionEntity();
        await RefreshTunnelListAsync().ConfigureAwait(true);
        ApplyModeState();
    }

    public void ActivateExecutionMode()
    {
        EnsureExecutionEntity();
        SetMode(NgrokWorkspaceMode.Execution, "Modo execução ativado.");
        _ = RefreshTunnelListAsync();
    }

    public void ActivateConfigurationMode()
    {
        SetMode(NgrokWorkspaceMode.Configuration, "Modo configuração ativado.");
        EnterInitialConfigurationState(showStatus: true);
    }

    private void SetMode(NgrokWorkspaceMode mode, string status)
    {
        _currentMode = mode;
        ValidationUiService.ClearInline(ExecutionStatusText);
        ExecutionStatusText.Text = status;
        ApplyModeState();
    }

    private async Task ReloadEntitiesAsync()
    {
        var list = await _facade.LoadAsync().ConfigureAwait(true);
        _entities.Clear();
        foreach (var item in list.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
            _entities.Add(item);
    }

    private void EnsureExecutionEntity()
    {
        if (_useCurrentEntityOnNextExecution && _currentEntity is not null)
        {
            _useCurrentEntityOnNextExecution = false;
            BindEntityToForm(_currentEntity);
            return;
        }

        if (_entities.Count == 0)
        {
            _currentEntity ??= new NgrokEntity();
            BindEntityToForm(_currentEntity);
            _baselineEntity = null;
            return;
        }

        var selected = _entities.FirstOrDefault(x => x.IsDefault) ?? _entities.First();
        _currentEntity = CloneEntity(selected);
        _baselineEntity = CloneEntity(selected);
        _isCreatingNew = false;
        _isDirty = false;
        BindEntityToForm(_currentEntity);
    }

    private void EnterInitialConfigurationState(bool showStatus)
    {
        _suppressGridSelection = true;
        ConfigurationsGrid.SelectedItem = null;
        _suppressGridSelection = false;

        _isCreatingNew = false;
        _isDirty = false;
        _baselineEntity = null;
        _currentEntity = new NgrokEntity();
        BindEntityToForm(_currentEntity);

        ClearInlineValidationStates();
        ValidationUiService.ClearInline(ExecutionStatusText);
        if (showStatus)
            ExecutionStatusText.Text = "Selecione uma configuração ou clique em Novo.";

        ApplyModeState();
    }

    private void LoadEntityFromGrid(NgrokEntity entity, bool focusForEditing)
    {
        _isCreatingNew = false;
        _isDirty = false;
        _currentEntity = CloneEntity(entity);
        _baselineEntity = CloneEntity(entity);
        BindEntityToForm(_currentEntity);

        ValidationUiService.ClearInline(ExecutionStatusText);
        ExecutionStatusText.Text = focusForEditing
            ? $"Modo edição iniciado para \"{entity.Name}\"."
            : $"Configuração \"{entity.Name}\" carregada.";

        if (focusForEditing)
        {
            NameInput.Focus();
            NameInput.SelectAll();
        }

        ApplyModeState();
    }

    private void ConfigurationsGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_suppressGridSelection || _currentMode != NgrokWorkspaceMode.Configuration)
            return;

        if (ConfigurationsGrid.SelectedItem is not NgrokEntity selected)
        {
            if (!_isCreatingNew)
                EnterInitialConfigurationState(showStatus: false);
            return;
        }

        LoadEntityFromGrid(selected, focusForEditing: false);
    }

    private void ConfigurationsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_currentMode != NgrokWorkspaceMode.Configuration)
            return;
        if (ConfigurationsGrid.SelectedItem is not NgrokEntity selected)
            return;

        LoadEntityFromGrid(selected, focusForEditing: true);
    }

    private void ConfigurationField_Changed(object sender, System.Windows.Controls.TextChangedEventArgs e)
        => HandleConfigurationFieldChange();

    private void ConfigurationField_Changed(object sender, RoutedEventArgs e)
        => HandleConfigurationFieldChange();

    private void ConfigurationField_Changed(object? sender, EventArgs e)
        => HandleConfigurationFieldChange();

    private void HandleConfigurationFieldChange()
    {
        if (_suppressFormTracking || _currentMode != NgrokWorkspaceMode.Configuration)
            return;

        var hasSelected = ConfigurationsGrid.SelectedItem is NgrokEntity;
        if (!_isCreatingNew && !hasSelected)
            return;

        _isDirty = _isCreatingNew || IsFormDifferentFromBaseline();
        ApplyModeState();
    }

    private bool IsFormDifferentFromBaseline()
    {
        if (_baselineEntity is null)
            return HasAnyConfigurationValue();

        var current = BuildEntityFromForm(_baselineEntity.Id, _baselineEntity.CreatedAtUtc);
        return !AreEquivalent(current, _baselineEntity);
    }

    private bool HasAnyConfigurationValue()
        => !string.IsNullOrWhiteSpace(NameInput.Text)
           || !string.IsNullOrWhiteSpace(DescriptionInput.Text)
           || !string.IsNullOrWhiteSpace(AuthTokenInput.Text)
           || !string.IsNullOrWhiteSpace(ExecutablePathSelector.SelectedPath)
           || !string.IsNullOrWhiteSpace(AdditionalArgsInput.Text)
           || !string.IsNullOrWhiteSpace(BaseUrlInput.Text)
           || (IsDefaultCheck.IsChecked ?? false);

    private void ActionNew_Click(object sender, RoutedEventArgs e)
    {
        if (_currentMode != NgrokWorkspaceMode.Configuration)
        {
            SetMode(NgrokWorkspaceMode.Configuration, "Modo configuração ativado.");
            EnterInitialConfigurationState(showStatus: false);
        }

        _suppressGridSelection = true;
        ConfigurationsGrid.SelectedItem = null;
        _suppressGridSelection = false;

        _isCreatingNew = true;
        _isDirty = false;
        _baselineEntity = null;
        _currentEntity = new NgrokEntity();
        BindEntityToForm(_currentEntity);

        ValidationUiService.ClearInline(ExecutionStatusText);
        ExecutionStatusText.Text = "Nova configuração.";
        NameInput.Focus();
        ApplyModeState();
    }

    private async void ActionSave_Click(object sender, RoutedEventArgs e)
    {
        if (_currentMode == NgrokWorkspaceMode.Execution)
        {
            await StartHttpFromActionBarAsync().ConfigureAwait(true);
            return;
        }

        if (!_isCreatingNew && !_isDirty)
            return;

        _currentEntity ??= new NgrokEntity();
        ReadFormIntoEntity();

        ValidationUiService.SetControlInvalid(NameInput, false);
        ValidationUiService.SetControlInvalid(AuthTokenInput, false);

        if (!ValidationUiService.ValidateRequiredFields(
                out var error,
                ValidationUiService.RequiredControl("Nome", NameInput, NameInput.Text),
                ValidationUiService.RequiredControl("Auth token do ngrok", AuthTokenInput, AuthTokenInput.Text)))
        {
            ValidationUiService.ShowInline(ExecutionStatusText, error);
            return;
        }

        var validation = await _facade.SaveAsync(_currentEntity).ConfigureAwait(true);
        if (!validation.IsValid)
        {
            ValidationUiService.ShowInline(
                ExecutionStatusText,
                string.Join(" | ", validation.Errors.Select(x => x.Message)));
            return;
        }

        await ReloadEntitiesAsync().ConfigureAwait(true);
        EnterInitialConfigurationState(showStatus: false);
        ExecutionStatusText.Text = "Configuração salva.";
    }

    private async void ActionDelete_Click(object sender, RoutedEventArgs e)
    {
        if (_currentMode != NgrokWorkspaceMode.Configuration)
            return;
        if (ConfigurationsGrid.SelectedItem is not NgrokEntity selected || string.IsNullOrWhiteSpace(selected.Id))
            return;

        var confirm = DevToolsMessageBox.Confirm(
            Window.GetWindow(this),
            "Deseja excluir esta configuração?",
            "Excluir");
        if (confirm != DevToolsMessageBoxResult.Yes)
            return;

        await _facade.DeleteAsync(selected.Id).ConfigureAwait(true);
        await ReloadEntitiesAsync().ConfigureAwait(true);
        EnterInitialConfigurationState(showStatus: false);
        ExecutionStatusText.Text = "Configuração excluída.";
    }

    private async void ActionCancel_Click(object sender, RoutedEventArgs e)
    {
        if (_currentMode == NgrokWorkspaceMode.Configuration)
        {
            EnterInitialConfigurationState(showStatus: false);
            ExecutionStatusText.Text = "Alterações canceladas.";
            return;
        }

        await StopAllFromActionBarAsync().ConfigureAwait(true);
    }

    private void ActionGoToTool_Click(object sender, RoutedEventArgs e)
    {
        if (_currentMode == NgrokWorkspaceMode.Configuration)
        {
            _currentEntity ??= new NgrokEntity();
            ReadFormIntoEntity();
            _useCurrentEntityOnNextExecution = true;
        }

        if (Window.GetWindow(this) is MainWindow mainWindow)
        {
            mainWindow.OpenToolExecution("Ngrok");
            return;
        }

        ActivateExecutionMode();
    }

    private void ActionBack_Click(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is MainWindow mainWindow)
            mainWindow.OpenFerramentasHome();
    }

    private async void HistoryButton_Click(object sender, RoutedEventArgs e)
        => await ToolHistoryViewHelper.ShowAndApplyAsync(
                WorkspaceRoot,
                ToolHistorySlug,
                ToolDisplayName,
                ExecutionStatusText)
            .ConfigureAwait(true);

    private async Task StopAllFromActionBarAsync()
    {
        if (!TryPrepareExecutionInputs(requireAuthToken: false, validateBaseUrl: false))
            return;

        var confirm = DevToolsMessageBox.Confirm(
            Window.GetWindow(this),
            "Encerrar todos os processos ngrok?",
            "Parar ngrok");
        if (confirm != DevToolsMessageBoxResult.Yes)
            return;

        await RunActionAsync(new NgrokRequest
        {
            Action = NgrokAction.KillAll,
            BaseUrl = GetBaseUrl()
        }).ConfigureAwait(true);
    }

    private bool TryPrepareExecutionInputs(bool requireAuthToken, bool validateBaseUrl)
    {
        _currentEntity ??= new NgrokEntity();
        ReadFormIntoEntity();

        ValidationUiService.SetControlInvalid(AuthTokenInput, false);
        ValidationUiService.SetControlInvalid(BaseUrlInput, false);

        if (requireAuthToken && string.IsNullOrWhiteSpace(_currentEntity.AuthToken))
        {
            ValidationUiService.SetControlInvalid(AuthTokenInput, true);
            ValidationUiService.ShowInline(ExecutionStatusText, "Auth token do ngrok é obrigatório para iniciar o túnel.");
            return false;
        }

        if (validateBaseUrl)
        {
            var baseUrl = GetBaseUrl();
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out _))
            {
                ValidationUiService.SetControlInvalid(BaseUrlInput, true);
                ValidationUiService.ShowInline(ExecutionStatusText, "URL da API local do ngrok inválida.");
                return false;
            }
        }

        return true;
    }

    private async Task StartHttpFromActionBarAsync()
    {
        if (!TryPrepareExecutionInputs(requireAuthToken: true, validateBaseUrl: false))
            return;

        ValidationUiService.SetControlInvalid(StartPortInput, false);
        if (!int.TryParse(StartPortInput.Text.Trim(), out var port) || port <= 0)
        {
            ValidationUiService.SetControlInvalid(StartPortInput, true);
            ValidationUiService.ShowInline(ExecutionStatusText, "Porta inválida.");
            return;
        }

        var protocol = ProtocolCombo.SelectedItem as string ?? "http";
        var execPath = _currentEntity?.ExecutablePath;
        var authToken = _currentEntity?.AuthToken;
        var extraArgs = ParseAdditionalArgs(_currentEntity?.AdditionalArgs);

        await RunActionAsync(new NgrokRequest
        {
            Action = NgrokAction.StartHttp,
            BaseUrl = GetBaseUrl(),
            StartOptions = new NgrokStartOptions(protocol, port, execPath, extraArgs, authToken)
        }).ConfigureAwait(true);
    }

    private async Task RefreshTunnelListAsync()
    {
        try
        {
            var tunnels = await _facade.GetActiveTunnelsAsync(GetBaseUrl()).ConfigureAwait(true);
            BindTunnelList(tunnels);
        }
        catch
        {
            BindTunnelList(Array.Empty<TunnelInfo>());
        }
    }

    private void BindTunnelList(IReadOnlyList<TunnelInfo> tunnels)
    {
        _activeTunnelCount = tunnels.Count;
        TunnelListPanel.Visibility = Visibility.Visible;
        TunnelList.ItemsSource = tunnels;
        TunnelListHeader.Text = tunnels.Count == 0
            ? "Túneis ativos"
            : $"Túneis ativos ({tunnels.Count})";
        EmptyStateText.Visibility = tunnels.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;

        ApplyModeState();
    }

    private async Task RunActionAsync(NgrokRequest request)
    {
        await ToolHistoryViewHelper.RecordAsync(
                ToolHistorySlug,
                WorkspaceRoot,
                $"Executar ngrok ({request.Action})")
            .ConfigureAwait(true);

        ExecutionStatusText.Text = "Executando...";

        try
        {
            var result = await _facade.ExecuteAsync(request).ConfigureAwait(true);
            if (!result.IsSuccess)
            {
                ValidationUiService.ShowInline(
                    ExecutionStatusText,
                    string.Join(" | ", result.Errors.Select(x => x.Message)));
                return;
            }

            ValidationUiService.ClearInline(ExecutionStatusText);
            var data = result.Value!;

            switch (data.Action)
            {
                case NgrokAction.StartHttp:
                    ExecutionStatusText.Text = data.ProcessId.HasValue
                        ? $"Ngrok iniciado. PID: {data.ProcessId.Value}"
                        : "Ngrok iniciado.";
                    break;

                case NgrokAction.KillAll:
                    ExecutionStatusText.Text = $"{data.Killed ?? 0} processo(s) encerrado(s).";
                    break;

                case NgrokAction.CloseTunnel:
                    ExecutionStatusText.Text = (data.Closed ?? false)
                        ? "Túnel fechado."
                        : "Túnel não encontrado.";
                    break;

                case NgrokAction.ListTunnels:
                    ExecutionStatusText.Text = $"Listados {(data.Tunnels?.Count ?? 0)} túnel(is).";
                    break;

                default:
                    ExecutionStatusText.Text = "Operação concluída.";
                    break;
            }

            if (data.Action == NgrokAction.StartHttp && Window.GetWindow(this) is MainWindow mainWindow)
                await mainWindow.MoveToBackgroundTrayAsync().ConfigureAwait(true);

            await RefreshTunnelListAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            ValidationUiService.ShowInline(ExecutionStatusText, ex.Message);
        }
    }

    private void CopyUrl_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button { Tag: string url } || string.IsNullOrWhiteSpace(url))
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

    private string GetBaseUrl()
        => _currentEntity?.BaseUrl?.Trim() is { Length: > 0 } url
            ? url
            : "http://127.0.0.1:4040/";

    private void BindEntityToForm(NgrokEntity entity)
    {
        _suppressFormTracking = true;
        _currentEntity = entity;
        NameInput.Text = entity.Name;
        DescriptionInput.Text = entity.Description;
        AuthTokenInput.Text = entity.AuthToken;
        ExecutablePathSelector.SelectedPath = entity.ExecutablePath;
        AdditionalArgsInput.Text = entity.AdditionalArgs;
        BaseUrlInput.Text = entity.BaseUrl ?? "http://127.0.0.1:4040/";
        IsDefaultCheck.IsChecked = entity.IsDefault;
        _suppressFormTracking = false;
    }

    private void ReadFormIntoEntity()
    {
        if (_currentEntity is null)
            return;

        _currentEntity.Name = NameInput.Text.Trim();
        _currentEntity.Description = DescriptionInput.Text.Trim();
        _currentEntity.AuthToken = AuthTokenInput.Text.Trim();
        _currentEntity.ExecutablePath = ExecutablePathSelector.SelectedPath?.Trim() ?? string.Empty;
        _currentEntity.AdditionalArgs = AdditionalArgsInput.Text.Trim();
        _currentEntity.BaseUrl = BaseUrlInput.Text.Trim();
        _currentEntity.IsDefault = IsDefaultCheck.IsChecked ?? false;
    }

    private NgrokEntity BuildEntityFromForm(string id, DateTime createdAtUtc)
        => new()
        {
            Id = id,
            Name = NameInput.Text.Trim(),
            Description = DescriptionInput.Text.Trim(),
            AuthToken = AuthTokenInput.Text.Trim(),
            ExecutablePath = ExecutablePathSelector.SelectedPath?.Trim() ?? string.Empty,
            AdditionalArgs = AdditionalArgsInput.Text.Trim(),
            BaseUrl = BaseUrlInput.Text.Trim(),
            IsDefault = IsDefaultCheck.IsChecked ?? false,
            CreatedAtUtc = createdAtUtc
        };

    private static NgrokEntity CloneEntity(NgrokEntity source)
        => new()
        {
            Id = source.Id,
            Name = source.Name,
            Description = source.Description,
            AuthToken = source.AuthToken,
            ExecutablePath = source.ExecutablePath,
            AdditionalArgs = source.AdditionalArgs,
            BaseUrl = source.BaseUrl,
            IsDefault = source.IsDefault,
            IsActive = source.IsActive,
            CreatedAtUtc = source.CreatedAtUtc,
            UpdatedAtUtc = source.UpdatedAtUtc
        };

    private static bool AreEquivalent(NgrokEntity left, NgrokEntity right)
        => string.Equals(left.Name, right.Name, StringComparison.Ordinal)
           && string.Equals(left.Description, right.Description, StringComparison.Ordinal)
           && string.Equals(left.AuthToken, right.AuthToken, StringComparison.Ordinal)
           && string.Equals(left.ExecutablePath, right.ExecutablePath, StringComparison.Ordinal)
           && string.Equals(left.AdditionalArgs, right.AdditionalArgs, StringComparison.Ordinal)
           && string.Equals(left.BaseUrl, right.BaseUrl, StringComparison.Ordinal)
           && left.IsDefault == right.IsDefault;

    private void ClearInlineValidationStates()
    {
        ValidationUiService.SetControlInvalid(NameInput, false);
        ValidationUiService.SetControlInvalid(AuthTokenInput, false);
        ValidationUiService.SetControlInvalid(BaseUrlInput, false);
        ValidationUiService.SetControlInvalid(StartPortInput, false);
    }

    private static IReadOnlyList<string>? ParseAdditionalArgs(string? rawArgs)
    {
        if (string.IsNullOrWhiteSpace(rawArgs))
            return null;

        var args = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        foreach (var ch in rawArgs)
        {
            if (ch == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (char.IsWhiteSpace(ch) && !inQuotes)
            {
                if (current.Length == 0)
                    continue;

                args.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(ch);
        }

        if (current.Length > 0)
            args.Add(current.ToString());

        return args.Count == 0 ? null : args;
    }

    private void ApplyModeState()
    {
        var inConfiguration = _currentMode == NgrokWorkspaceMode.Configuration;
        var inExecution = _currentMode == NgrokWorkspaceMode.Execution;
        var hasSelection = ConfigurationsGrid.SelectedItem is NgrokEntity;
        var formEnabled = inExecution || _isCreatingNew || hasSelection;

        ConfigurationListSection.Visibility = inConfiguration ? Visibility.Visible : Visibility.Collapsed;
        ConfigurationFormSection.IsEnabled = formEnabled;
        ExecutionSection.Visibility = inExecution ? Visibility.Visible : Visibility.Collapsed;

        WorkspaceTitleText.Text = inConfiguration ? "Ngrok - Configuração" : "Ngrok";
        WorkspaceSubtitleText.Text = inConfiguration
            ? "Grid para selecionar e formulário para criar/editar configurações."
            : "Gerencia túneis HTTP/HTTPS via ngrok.";

        Actions.NewText = "Novo";
        Actions.SaveText = inConfiguration ? "Salvar" : "Executar";
        Actions.SaveIconKind = inConfiguration ? "ContentSave" : "Play";
        Actions.CancelText = inConfiguration ? "Cancelar" : "Parar ngrok";
        Actions.DeleteText = "Deletar";
        Actions.GoToToolText = "Ir para ferramenta";
        Actions.BackText = "Voltar";
        Actions.BackIconKind = "ArrowLeft";

        Actions.ShowHelp = true;
        Actions.HelpContextKey = inConfiguration ? "ngrok:configuration" : "ngrok:execution";
        Actions.ShowHistory = inExecution;
        Actions.ShowNew = inConfiguration;
        Actions.ShowSave = inConfiguration || inExecution;
        Actions.ShowCancel = inConfiguration || inExecution;
        Actions.ShowDelete = inConfiguration;
        Actions.ShowGoToTool = inConfiguration;
        Actions.ShowBack = inExecution;
        Actions.Visibility = Visibility.Visible;

        if (inConfiguration)
        {
            var canSave = _isCreatingNew || _isDirty;
            Actions.CanNew = true;
            Actions.CanSave = canSave;
            Actions.CanCancel = canSave;
            Actions.CanDelete = hasSelection && !_isCreatingNew;
            Actions.CanGoToTool = true;
            Actions.CanBack = false;
            Actions.CanHistory = false;
        }
        else
        {
            Actions.CanNew = false;
            Actions.CanSave = _currentEntity is not null;
            Actions.CanCancel = _activeTunnelCount > 0;
            Actions.CanDelete = false;
            Actions.CanGoToTool = false;
            Actions.CanBack = true;
            Actions.CanHistory = true;
        }

        Actions.CanHelp = true;
    }
}
