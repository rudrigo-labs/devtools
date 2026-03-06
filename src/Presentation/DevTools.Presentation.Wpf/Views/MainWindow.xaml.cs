using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DevTools.Presentation.Wpf.Services;
using DevTools.Presentation.Wpf.ToolRouting;
using DevTools.SSHTunnel.Models;
using DevTools.Harvest.Configuration;
using DevTools.Organizer.Models;
using DevTools.Migrations.Models;
using DevTools.Ngrok.Engine;
using DevTools.Ngrok.Models;
using DevTools.Presentation.Wpf.Models;
using DevTools.Presentation.Wpf.Persistence;
using DevTools.Presentation.Wpf.Components;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DevTools.Core.Models;
using System.Windows.Media;

namespace DevTools.Presentation.Wpf.Views;

public partial class MainWindow : Window
{
    private const string StorageBackendEnvVar = "DEVTOOLS_STORAGE_BACKEND";
    private static readonly string[] BlockedMigrationsArgs =
    {
        "--project", "-p", "project",
        "--startup-project", "-s", "startup-project", "startupproject",
        "--context", "-c", "context", "dbcontext"
    };

    private readonly TrayService _trayService;
    private readonly JobManager _jobManager;
    private readonly ConfigService _configService;
    private readonly GoogleDriveService _googleDriveService;
    private readonly NgrokSetupService _ngrokSetupService;

    // Config Objects
    private HarvestConfig _currentHarvestConfig = new();
    private OrganizerConfig _currentOrganizerConfig = new();
    private MigrationsSettings _currentMigrationsConfig = new();
    private NgrokSettings _currentNgrokConfig = new();
    private NotesSettings _currentNotesSettings = new();
    private GoogleDriveSettings _currentGoogleDriveSettings = new();

    // State
    private OrganizerCategory? _selectedCategory;
    private string? _currentToolForConfigurations;
    private ToolConfiguration? _selectedToolConfiguration;
    private readonly HashSet<ToolConfiguration> _pendingNewToolConfigurations = new();
    private readonly Dictionary<ToolConfiguration, string> _persistedToolConfigurationNames = new();
    private OrganizerCategory? _pendingNewOrganizerCategory;
    private readonly ToolConfigurationUIService _toolConfigurationUIService;
    private bool _isTestingGoogleDriveConnection;
    private bool _allowCloseForShutdown;
    private string? _currentEmbeddedToolId;
    private bool _isSyncingOwnedWindows;
    private bool _hasTrackedMainLocation;
    private double _lastMainLeft;
    private double _lastMainTop;

    public MainWindow(TrayService trayService, JobManager jobManager, ConfigService configService, ToolConfigurationUIService toolConfigurationUIService, GoogleDriveService googleDriveService)
    {
        InitializeComponent();
        _trayService = trayService;
        _jobManager = jobManager;
        _configService = configService;
        _toolConfigurationUIService = toolConfigurationUIService;
        _googleDriveService = googleDriveService;
        _ngrokSetupService = new NgrokSetupService();

        _trayService.SetMainWindow(this);
        _trayService.EmbeddedToolRequested += TrayService_EmbeddedToolRequested;

        // Binding direto da colecao de Jobs
        JobsDataGrid.ItemsSource = _jobManager.Jobs;

        this.Loaded += MainWindow_Loaded;
        this.Closing += MainWindow_Closing;
        this.IsVisibleChanged += MainWindow_IsVisibleChanged;
        this.StateChanged += MainWindow_StateChanged;
        this.LocationChanged += MainWindow_LocationChanged;
    }

    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        UpdateMaximizeRestoreButton();
    }

    private void MainWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        // Se a MainWindow for escondida (Hide), temos que esconder a ferramenta atual também
        // para que ela não fique "órfã" na tela
        if (this.Visibility != Visibility.Visible)
        {
            _trayService.OpenTool("HIDE_CURRENT"); // Comando interno para esconder se necessario
        }
    }

    // Construtor para o Designer
    public MainWindow()
    {
        InitializeComponent();
        _trayService = null!;
        _jobManager = null!;
        _configService = null!;
        _toolConfigurationUIService = null!;
        _googleDriveService = null!;
        _ngrokSetupService = new NgrokSetupService();
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        ApplyMainWindowWorkingArea();
        UpdateMaximizeRestoreButton();
    }

    public void ResetToHome()
    {
        MainTabControl.SelectedItem = TabTools;
        ShowSettingsList();
    }

    public void AllowCloseForShutdown()
    {
        _allowCloseForShutdown = true;
    }

    public void ShowEmbeddedTool(EmbeddedToolRequest request)
    {
        switch (request.Descriptor.EmbeddedTarget)
        {
            case EmbeddedToolTarget.ToolsHome:
                ResetToHome();
                return;
            case EmbeddedToolTarget.JobsTab:
                MainTabControl.SelectedItem = TabJobs;
                return;
            case EmbeddedToolTarget.EmbeddedHost:
                if (request.Descriptor.Singleton
                    && string.Equals(_currentEmbeddedToolId, request.Descriptor.Id, StringComparison.OrdinalIgnoreCase))
                {
                    MainTabControl.SelectedItem = TabEmbeddedTool;
                    return;
                }

                _currentEmbeddedToolId = request.Descriptor.Id;
                EmbeddedToolTitleText.Text = request.Descriptor.Title;
                EmbeddedToolSubtitleText.Text = string.IsNullOrWhiteSpace(request.Descriptor.Subtitle)
                    ? "Ferramenta embutida no shell principal."
                    : request.Descriptor.Subtitle;
                EmbeddedToolContentHost.Content = request.Content ?? CreateEmbeddedFallback(request.Descriptor.Title);
                MainTabControl.SelectedItem = TabEmbeddedTool;
                return;
            default:
                ResetToHome();
                return;
        }
    }

    private void NavButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is string tag)
        {
            switch (tag)
            {
                case "Tools":
                    MainTabControl.SelectedItem = TabTools;
                    break;
                case "Jobs":
                    MainTabControl.SelectedItem = TabJobs;
                    break;
                case "Settings":
                    MainTabControl.SelectedItem = TabSettings;
                    break;
            }
        }
    }

    private void OpenTool_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.CommandParameter is string toolTag)
        {
            _trayService.OpenTool(toolTag);
        }
    }

    private void OpenAbout_Click(object sender, RoutedEventArgs e)
    {
        var aboutWindow = new AboutWindow
        {
            Owner = this
        };

        aboutWindow.ShowDialog();
    }

    private void BackToToolsFromEmbedded_Click(object sender, RoutedEventArgs e)
    {
        MainTabControl.SelectedItem = TabTools;
    }

    private void TrayService_EmbeddedToolRequested(EmbeddedToolRequest request)
    {
        Dispatcher.Invoke(() => ShowEmbeddedTool(request));
    }

    private TextBlock CreateEmbeddedFallback(string title)
    {
        return new TextBlock
        {
            Text = $"A ferramenta '{title}' não possui conteúdo embutido configurado.",
            Foreground = (System.Windows.Media.Brush)FindResource("DevToolsTextSecondary"),
            Margin = new Thickness(24),
            FontSize = 14,
            TextWrapping = TextWrapping.Wrap
        };
    }

    /// <summary>
    /// Botao Encerrar escolha:Sim
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void Shutdown_Click(object sender, RoutedEventArgs e)
    {
        MaterialDesignThemes.Wpf.DialogHost.Close("RootDialog");
        await _trayService.RequestExitAsync(skipConfirmation: true);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        CloseDialogMessageText.Text = BuildCloseDialogMessage();
        UpdateCloseDialogActions();
        var dialogContent = RootDialog.DialogContent ?? RootDialog;
        MaterialDesignThemes.Wpf.DialogHost.Show(dialogContent, "RootDialog");
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        // MainWindow fixa em tela cheia por regra de UX.
    }

    private void MinimizeToTray_Click(object sender, RoutedEventArgs e)
    {
        if (!_trayService.HasActiveTunnel)
        {
            ShowMainStatusInfo("Minimizar para bandeja disponível somente com túnel SSH ativo.");
            return;
        }

        _trayService.EnsureInitialized();
        Hide();
    }

    private void MinimizeToTrayFromDialog_Click(object sender, RoutedEventArgs e)
    {
        if (!_trayService.HasActiveTunnel)
        {
            MaterialDesignThemes.Wpf.DialogHost.Close("RootDialog");
            ShowMainStatusInfo("Minimizar para bandeja disponível somente com túnel SSH ativo.");
            return;
        }

        MaterialDesignThemes.Wpf.DialogHost.Close("RootDialog");
        _trayService.EnsureInitialized();
        Hide();
    }

    private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
    {
        // MainWindow fixa em tela cheia por regra de UX.
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
        {
            return;
        }

        try
        {
            DragMove();
        }
        catch
        {
            // Ignora interrupcao de drag.
        }
        ApplyMainWindowWorkingArea();
        CenterOwnedWindowsOnMain();
    }

    private void MainWindow_LocationChanged(object? sender, EventArgs e)
    {
        if (_isSyncingOwnedWindows || OwnedWindows.Count == 0)
        {
            _lastMainLeft = Left;
            _lastMainTop = Top;
            _hasTrackedMainLocation = true;
            return;
        }

        if (!_hasTrackedMainLocation)
        {
            _lastMainLeft = Left;
            _lastMainTop = Top;
            _hasTrackedMainLocation = true;
            return;
        }

        var deltaX = Left - _lastMainLeft;
        var deltaY = Top - _lastMainTop;

        if (Math.Abs(deltaX) < 0.01 && Math.Abs(deltaY) < 0.01)
        {
            return;
        }

        _isSyncingOwnedWindows = true;
        try
        {
            foreach (Window owned in OwnedWindows)
            {
                if (!owned.IsVisible)
                {
                    continue;
                }

                owned.Left += deltaX;
                owned.Top += deltaY;
            }
        }
        finally
        {
            _isSyncingOwnedWindows = false;
            _lastMainLeft = Left;
            _lastMainTop = Top;
        }
    }

    private void ApplyMainWindowWorkingArea()
    {
        var workingArea = GetCurrentWorkingArea();

        _isSyncingOwnedWindows = true;
        try
        {
            WindowState = WindowState.Normal;
            if (MinWidth > workingArea.Width)
            {
                MinWidth = workingArea.Width;
            }

            if (MinHeight > workingArea.Height)
            {
                MinHeight = workingArea.Height;
            }

            Left = workingArea.Left;
            Top = workingArea.Top;
            Width = workingArea.Width;
            Height = workingArea.Height;
            MaxWidth = workingArea.Width;
            MaxHeight = workingArea.Height;
        }
        finally
        {
            _isSyncingOwnedWindows = false;
            _lastMainLeft = Left;
            _lastMainTop = Top;
            _hasTrackedMainLocation = true;
        }
    }

    private Rect GetCurrentWorkingArea()
    {
        var centerPoint = new System.Drawing.Point(
            (int)Math.Round(Left + (Width / 2)),
            (int)Math.Round(Top + (Height / 2)));

        var screen = System.Windows.Forms.Screen.FromPoint(centerPoint);
        var area = screen.WorkingArea;
        return new Rect(area.Left, area.Top, area.Width, area.Height);
    }

    private void CenterOwnedWindowsOnMain()
    {
        if (OwnedWindows.Count == 0)
        {
            return;
        }

        _isSyncingOwnedWindows = true;
        try
        {
            foreach (Window owned in OwnedWindows)
            {
                if (!owned.IsVisible)
                {
                    continue;
                }

                owned.Left = Left + ((ActualWidth - owned.ActualWidth) / 2);
                owned.Top = Top + ((ActualHeight - owned.ActualHeight) / 2);
            }
        }
        finally
        {
            _isSyncingOwnedWindows = false;
        }
    }

    private void DialogNoButton_Click(object sender, RoutedEventArgs e)
    {
        MaterialDesignThemes.Wpf.DialogHost.Close("RootDialog");
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_allowCloseForShutdown)
        {
            if (_trayService != null)
            {
                _trayService.EmbeddedToolRequested -= TrayService_EmbeddedToolRequested;
            }

            return;
        }

        // Se o usuario clicar no X ou Alt+F4, mostramos o dialogo em vez de esconder
        e.Cancel = true;
        CloseButton_Click(this, new RoutedEventArgs());
    }

    private string BuildCloseDialogMessage()
    {
        var details = new List<string>();
        if (_jobManager.RunningJobsCount > 0)
        {
            details.Add($"- Jobs em execução: {_jobManager.RunningJobsCount}");
        }

        if (_trayService.HasActiveTunnel)
        {
            details.Add("- Tunel SSH ativo");
        }

        if (details.Count == 0)
        {
            return "Nenhuma operação ativa detectada. Escolha uma opção.";
        }

        return "Operacoes ativas detectadas:\n"
            + string.Join("\n", details)
            + "\n\nEscolha como deseja continuar.";
    }

    private void ToggleMaximizeRestore()
    {
        ApplyMainWindowWorkingArea();
    }

    private void UpdateMaximizeRestoreButton()
    {
        if (MaximizeRestoreButton == null)
        {
            return;
        }

        MaximizeRestoreButton.Content = WindowState == WindowState.Maximized
            ? "\uE923"
            : "\uE922";
    }

    private void UpdateCloseDialogActions()
    {
        if (CloseDialogMinimizeToTrayButton == null)
        {
            return;
        }

        CloseDialogMinimizeToTrayButton.Visibility = _trayService.HasActiveTunnel
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    // --- Settings Navigation Logic ---

    private void ShowSettingsList()
    {
        SettingsListPanel.Visibility = Visibility.Visible;
        

        StorageSettingsPanel.Visibility = Visibility.Collapsed;
        HarvestSettingsPanel.Visibility = Visibility.Collapsed;
        OrganizerSettingsPanel.Visibility = Visibility.Collapsed;
        MigrationsSettingsPanel.Visibility = Visibility.Collapsed;
        NgrokSettingsPanel.Visibility = Visibility.Collapsed;
        NotesCloudSettingsPanel.Visibility = Visibility.Collapsed;
        ToolConfigurationsSettingsPanel.Visibility = Visibility.Collapsed;
    }

    private void OpenStorageSettings_Click(object sender, RoutedEventArgs e)
    {
        SettingsListPanel.Visibility = Visibility.Collapsed;
        StorageSettingsPanel.Visibility = Visibility.Visible;
        LoadStorageSettingsConfig();
    }

    private void LoadStorageSettingsConfig()
    {
        var currentBackend = StorageBackendResolver.Resolve() == StorageBackend.Sqlite
            ? "sqlite"
            : "json";

        foreach (ComboBoxItem item in StorageBackendCombo.Items)
        {
            if (string.Equals(item.Tag?.ToString(), currentBackend, StringComparison.OrdinalIgnoreCase))
            {
                StorageBackendCombo.SelectedItem = item;
                break;
            }
        }

        UpdateStorageBackendHint(currentBackend);
    }

    private void StorageBackendCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateStorageBackendHint(GetSelectedStorageBackend());
    }

    private string GetSelectedStorageBackend()
    {
        var selectedTag = (StorageBackendCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        return string.Equals(selectedTag, "sqlite", StringComparison.OrdinalIgnoreCase)
            ? "sqlite"
            : "json";
    }

    private void UpdateStorageBackendHint(string backend)
    {
        if (StorageBackendHintText == null)
            return;

        if (string.Equals(backend, "sqlite", StringComparison.OrdinalIgnoreCase))
        {
            var dbPath = new SqlitePathProvider().GetDatabasePath();
            StorageBackendHintText.Text = $"SQLite selecionado. Banco em: {dbPath}";
            return;
        }

        var jsonPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        StorageBackendHintText.Text = $"Arquivo JSON selecionado. Config em: {jsonPath}";
    }

    private void SaveStorageSettings_Click(object sender, RoutedEventArgs e)
    {
        var storageBackendMissing = StorageBackendCombo.SelectedItem == null;
        SetControlValidationState(StorageBackendCombo, storageBackendMissing);

        if (storageBackendMissing)
        {
            ShowRequiredFieldsWarning("Os campos abaixo não podem ficar em branco:\n- Backend de Armazenamento");
            return;
        }

        var selectedBackend = GetSelectedStorageBackend();
        var currentBackend = StorageBackendResolver.Resolve() == StorageBackend.Sqlite
            ? "sqlite"
            : "json";

        try
        {
            Environment.SetEnvironmentVariable(StorageBackendEnvVar, selectedBackend, EnvironmentVariableTarget.User);
            Environment.SetEnvironmentVariable(StorageBackendEnvVar, selectedBackend, EnvironmentVariableTarget.Process);

            if (string.Equals(selectedBackend, currentBackend, StringComparison.OrdinalIgnoreCase))
            {
                UiMessageService.ShowInfo("Backend mantido com sucesso.", "Sucesso");
                ShowMainStatusInfo("Configuração salva com sucesso.");
                return;
            }

            UiMessageService.ShowInfo("Backend salvo. Reinicie o DevTools para aplicar a troca entre JSON e SQLite.", "Reinício necessário");
            ShowMainStatusInfo("Backend salvo. Reinicie o DevTools para aplicar a troca.");
        }
        catch (Exception ex)
        {
            UiMessageService.ShowError("Não foi possível salvar o backend de armazenamento.", "Erro ao salvar", ex);
        }
    }

    private void BackToSettingsList_Click(object sender, RoutedEventArgs e)
    {
        ShowSettingsList();
    }

    // --- Generic Tool Configurations Management ---

    private void OpenToolConfigurations_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.CommandParameter is string toolKey)
        {
            ShowSettingsList();
            _currentToolForConfigurations = toolKey;
            ApplyToolConfigurationTerminology(toolKey, btn.Content?.ToString());
            SettingsListPanel.Visibility = Visibility.Collapsed;
            ToolConfigurationsSettingsPanel.Visibility = Visibility.Visible;
            LoadToolConfigurations();
        }
    }

    private void ApplyToolConfigurationTerminology(string toolKey, string? displayName)
    {
        var text = GetToolConfigurationTerminology(toolKey, displayName);

        ToolConfigurationsTitle.Text = text.Title;

        if (ToolConfigurationEditorSectionTitle != null)
            ToolConfigurationEditorSectionTitle.Text = text.EditorSection;

        if (ToolConfigurationNameLabel != null)
            ToolConfigurationNameLabel.Text = text.NameLabel;

        if (ToolConfigurationIsDefaultCheck != null)
            ToolConfigurationIsDefaultCheck.Content = text.DefaultToggleLabel;

        // ActionBarControl usa labels internas do próprio componente.
    }

    private bool IsMigrationsConfigurationMode()
    {
        return string.Equals(_currentToolForConfigurations, "Migrations", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsProjectConfigurationMode()
    {
        return IsProjectConfigurationMode(_currentToolForConfigurations);
    }

    private static bool IsProjectConfigurationMode(string? toolKey)
    {
        return string.Equals(toolKey, "Migrations", StringComparison.OrdinalIgnoreCase)
            || string.Equals(toolKey, "Snapshot", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSshConnectionMode(string? toolKey)
    {
        return string.Equals(toolKey, "SSHTunnel", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsNgrokConnectionMode(string? toolKey)
    {
        return string.Equals(toolKey, "Ngrok", StringComparison.OrdinalIgnoreCase);
    }

    private static (string Title, string EditorSection, string NameLabel, string DefaultToggleLabel) GetToolConfigurationTerminology(string? toolKey, string? displayName)
    {
        if (IsProjectConfigurationMode(toolKey))
        {
            var title = string.Equals(toolKey, "Migrations", StringComparison.OrdinalIgnoreCase)
                ? "Projetos: EF Core Migrations"
                : $"Projetos: {displayName ?? toolKey}";
            return (
                title,
                "Configurações do Projeto",
                "Nome do Projeto",
                "Usar como projeto padrão");
        }

        if (IsSshConnectionMode(toolKey))
        {
            return (
                $"Túneis SSH: {displayName ?? toolKey}",
                "Configurações do Túnel",
                "Nome do Túnel",
                "Usar como túnel padrão");
        }

        if (IsNgrokConnectionMode(toolKey))
        {
            return (
                $"Conexões Ngrok: {displayName ?? toolKey}",
                "Configurações da Conexão",
                "Nome da Conexão",
                "Usar como conexão padrão");
        }

        return (
            $"Configuracoes: {displayName ?? toolKey}",
            "Configurações da Execução",
            "Nome da Configuração",
            "Usar como configuração padrão");
    }

    private void LoadToolConfigurations()
    {
        if (string.IsNullOrEmpty(_currentToolForConfigurations)) return;

        var configurations = _toolConfigurationUIService.LoadConfigurations(_currentToolForConfigurations);
        NormalizeConfigurationDefaultsInMemory(configurations);
        _pendingNewToolConfigurations.Clear();
        _persistedToolConfigurationNames.Clear();
        foreach (var configuration in configurations)
        {
            _persistedToolConfigurationNames[configuration] = configuration.Name;
        }

        ToolConfigurationsList.ItemsSource = null;
        ToolConfigurationsList.ItemsSource = configurations;

        if (configurations.Count == 0)
        {
            ToolConfigurationsList.SelectedItem = null;
            _selectedToolConfiguration = null;
            ToolConfigurationEditForm.Visibility = Visibility.Collapsed;
            UpdateToolConfigurationActionButtonsState();
            return;
        }

        var selected = configurations.FirstOrDefault(p => p.IsDefault) ?? configurations.First();
        SelectAndDisplayToolConfiguration(selected);
        UpdateToolConfigurationActionButtonsState();
    }

    private void AddToolConfiguration_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_currentToolForConfigurations)) return;

        var newConfiguration = new ToolConfiguration
        {
            Name = GenerateNextConfigurationName((ToolConfigurationsList.ItemsSource as List<ToolConfiguration>) ?? new List<ToolConfiguration>(), _currentToolForConfigurations)
        };

        var list = (ToolConfigurationsList.ItemsSource as List<ToolConfiguration>) ?? new List<ToolConfiguration>();
        list.Add(newConfiguration);
        _pendingNewToolConfigurations.Add(newConfiguration);

        ToolConfigurationsList.ItemsSource = null;
        ToolConfigurationsList.ItemsSource = list;
        SelectAndDisplayToolConfiguration(newConfiguration);
        UpdateToolConfigurationActionButtonsState();
    }

    private void ToolConfiguration_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ToolConfigurationsList.SelectedItem is ToolConfiguration configuration)
        {
            DisplayToolConfiguration(configuration);
        }
        else
        {
            _selectedToolConfiguration = null;
            ToolConfigurationEditForm.Visibility = Visibility.Collapsed;
        }

        UpdateToolConfigurationActionButtonsState();
    }

    private void SaveToolConfiguration_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedToolConfiguration == null || string.IsNullOrEmpty(_currentToolForConfigurations)) return;

        var configurationNameMissing = string.IsNullOrWhiteSpace(ToolConfigurationNameInput.Text);
        SetControlValidationState(ToolConfigurationNameInput, configurationNameMissing);
        if (configurationNameMissing)
        {
            var fieldLabel = GetToolConfigurationTerminology(_currentToolForConfigurations, null).NameLabel;
            ShowRequiredFieldsWarning($"Os campos abaixo não podem ficar em branco:\n- {fieldLabel}");
            return;
        }

        _selectedToolConfiguration.Name = ToolConfigurationNameInput.Text.Trim();
        _selectedToolConfiguration.IsDefault = ToolConfigurationIsDefaultCheck.IsChecked == true;

        if (ShouldForceSingleConfigurationAsDefault())
        {
            _selectedToolConfiguration.IsDefault = true;
            ToolConfigurationIsDefaultCheck.IsChecked = true;
        }

        if (!_selectedToolConfiguration.IsDefault && !HasOtherDefaultConfiguration())
        {
            var defaultLabel = GetToolConfigurationTerminology(_currentToolForConfigurations, null).DefaultToggleLabel;
            ShowRequiredFieldsWarning($"Para salvar, marque '{defaultLabel}'. Deve existir pelo menos um padrão.");
            return;
        }

        // Coletar valores dos campos dinamicos recursivamente
        CollectConfigurationOptions(ToolConfigurationFieldsContainer, _selectedToolConfiguration.Options);

        if (IsMigrationsConfigurationMode())
        {
            _selectedToolConfiguration.Options.TryGetValue("additional-args", out var configurationAdditionalArgs);
            if (!TryValidateMigrationsAdditionalArgs(configurationAdditionalArgs, out var configurationArgsError))
            {
                ShowRequiredFieldsWarning(configurationArgsError);
                return;
            }
        }

        var isPendingNew = _pendingNewToolConfigurations.Contains(_selectedToolConfiguration);
        if (!isPendingNew
            && _persistedToolConfigurationNames.TryGetValue(_selectedToolConfiguration, out var persistedName)
            && !string.Equals(persistedName, _selectedToolConfiguration.Name, StringComparison.OrdinalIgnoreCase))
        {
            _toolConfigurationUIService.DeleteConfiguration(_currentToolForConfigurations, persistedName);
        }

        _toolConfigurationUIService.SaveConfiguration(_currentToolForConfigurations, _selectedToolConfiguration);
        if (isPendingNew)
        {
            _pendingNewToolConfigurations.Remove(_selectedToolConfiguration);
        }
        _persistedToolConfigurationNames[_selectedToolConfiguration] = _selectedToolConfiguration.Name;

        ToolConfigurationsList.Items.Refresh();
        var successText = GetToolConfigurationSuccessMessage();
        UiMessageService.ShowInfo(successText, "Sucesso");
        ShowMainStatusInfo(successText);
        UpdateToolConfigurationActionButtonsState();
    }

    private void CollectConfigurationOptions(DependencyObject container, Dictionary<string, string> options)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(container); i++)
        {
            var child = VisualTreeHelper.GetChild(container, i);

            if (child is System.Windows.Controls.TextBox tb && tb.Tag is string key)
            {
                options[key] = tb.Text;
            }
            else if (child is Components.PathSelector ps && ps.Tag is string pathKey)
            {
                options[pathKey] = ps.SelectedPath;
            }
            else if (child is System.Windows.Controls.CheckBox checkBox && checkBox.Tag is string boolKey)
            {
                options[boolKey] = (checkBox.IsChecked == true).ToString();
            }
            else if (child is DependencyObject depObj)
            {
                // Busca recursiva em containers (Grid, StackPanel, Card, etc.)
                CollectConfigurationOptions(depObj, options);
            }
        }
    }

    private void DeleteToolConfiguration_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedToolConfiguration == null || string.IsNullOrEmpty(_currentToolForConfigurations)) return;

        var selectedConfiguration = _selectedToolConfiguration;
        var list = (ToolConfigurationsList.ItemsSource as List<ToolConfiguration>) ?? new List<ToolConfiguration>();
        var selectedIndex = ToolConfigurationsList.SelectedIndex;
        var isPendingNew = _pendingNewToolConfigurations.Contains(selectedConfiguration);

        var entityLower = GetToolConfigurationEntityNameLower();
        if (UiMessageService.Confirm($"Excluir {entityLower} '{selectedConfiguration.Name}'?", "Confirmar"))
        {
            if (!isPendingNew)
            {
                var persistedName = _persistedToolConfigurationNames.TryGetValue(selectedConfiguration, out var currentPersistedName)
                    ? currentPersistedName
                    : selectedConfiguration.Name;
                _toolConfigurationUIService.DeleteConfiguration(_currentToolForConfigurations, persistedName);
            }

            _pendingNewToolConfigurations.Remove(selectedConfiguration);
            _persistedToolConfigurationNames.Remove(selectedConfiguration);
            list.Remove(selectedConfiguration);

            ToolConfigurationsList.ItemsSource = null;
            ToolConfigurationsList.ItemsSource = list;

            if (list.Count == 0)
            {
                ToolConfigurationsList.SelectedItem = null;
                _selectedToolConfiguration = null;
                ToolConfigurationEditForm.Visibility = Visibility.Collapsed;
            }
            else
            {
                var nextIndex = selectedIndex;
                if (nextIndex < 0 || nextIndex >= list.Count)
                {
                    nextIndex = list.Count - 1;
                }

                var nextConfiguration = list[nextIndex];
                SelectAndDisplayToolConfiguration(nextConfiguration);
            }
        }

        UpdateToolConfigurationActionButtonsState();
    }



    // --- Harvest Settings ---

    private void OpenHarvestSettings_Click(object sender, RoutedEventArgs e)
    {
        SettingsListPanel.Visibility = Visibility.Collapsed;
        HarvestSettingsPanel.Visibility = Visibility.Visible;
        LoadHarvestConfig();
    }

    private void LoadHarvestConfig()
    {
        _currentHarvestConfig = _configService.GetSection<HarvestConfig>("Harvest");
        _currentHarvestConfig.Normalize();

        if (_currentHarvestConfig.Rules.ExcludeDirectories.Count == 0)
        {
            _currentHarvestConfig.Rules.ExcludeDirectories = HarvestDefaults.DefaultExcludeDirectories.ToList();
        }

        // Rules
        HarvestExtensions.Text = string.Join(", ", _currentHarvestConfig.Rules.Extensions);
        HarvestExcludeDirs.Text = string.Join(", ", _currentHarvestConfig.Rules.ExcludeDirectories);
        HarvestMaxFileSizeInput.Text = _currentHarvestConfig.Rules.MaxFileSizeKb?.ToString() ?? "";

        // Limits
        HarvestMinScore.Text = _currentHarvestConfig.MinScoreDefault.ToString();
        HarvestTopDefault.Text = _currentHarvestConfig.TopDefault.ToString();

        // Weights
        HarvestWeightFanIn.Text = _currentHarvestConfig.Weights.FanInWeight.ToString("F1");
        HarvestWeightFanOut.Text = _currentHarvestConfig.Weights.FanOutWeight.ToString("F1");
        HarvestWeightDensity.Text = _currentHarvestConfig.Weights.KeywordDensityWeight.ToString("F1");
        HarvestWeightDeadCode.Text = _currentHarvestConfig.Weights.DeadCodePenalty.ToString("F1");
    }

    private void SaveHarvestSettings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var extensionsMissing = string.IsNullOrWhiteSpace(HarvestExtensions.Text);
            var excludeDirsMissing = string.IsNullOrWhiteSpace(HarvestExcludeDirs.Text);
            var maxFileSizeMissing = string.IsNullOrWhiteSpace(HarvestMaxFileSizeInput.Text);
            var minScoreMissing = string.IsNullOrWhiteSpace(HarvestMinScore.Text);
            var topDefaultMissing = string.IsNullOrWhiteSpace(HarvestTopDefault.Text);
            var fanInMissing = string.IsNullOrWhiteSpace(HarvestWeightFanIn.Text);
            var fanOutMissing = string.IsNullOrWhiteSpace(HarvestWeightFanOut.Text);
            var densityMissing = string.IsNullOrWhiteSpace(HarvestWeightDensity.Text);
            var deadCodeMissing = string.IsNullOrWhiteSpace(HarvestWeightDeadCode.Text);

            SetControlValidationState(HarvestExtensions, extensionsMissing);
            SetControlValidationState(HarvestExcludeDirs, excludeDirsMissing);
            SetControlValidationState(HarvestMaxFileSizeInput, maxFileSizeMissing);
            SetControlValidationState(HarvestMinScore, minScoreMissing);
            SetControlValidationState(HarvestTopDefault, topDefaultMissing);
            SetControlValidationState(HarvestWeightFanIn, fanInMissing);
            SetControlValidationState(HarvestWeightFanOut, fanOutMissing);
            SetControlValidationState(HarvestWeightDensity, densityMissing);
            SetControlValidationState(HarvestWeightDeadCode, deadCodeMissing);

            var missingFields = new List<string>();
            if (extensionsMissing)
                missingFields.Add("Extensoes permitidas");
            if (excludeDirsMissing)
                missingFields.Add("Pastas excluidas");
            if (maxFileSizeMissing)
                missingFields.Add("Tamanho maximo por arquivo (KB)");
            if (minScoreMissing)
                missingFields.Add("Score minimo");
            if (topDefaultMissing)
                missingFields.Add("Top N default");
            if (fanInMissing)
                missingFields.Add("Peso FanIn");
            if (fanOutMissing)
                missingFields.Add("Peso FanOut");
            if (densityMissing)
                missingFields.Add("Peso Keyword Density");
            if (deadCodeMissing)
                missingFields.Add("Peso DeadCode");

            if (!TryBuildRequiredFieldsMessage(missingFields, out var requiredMessage))
            {
                ShowRequiredFieldsWarning(requiredMessage);
                return;
            }

            if (!int.TryParse(HarvestMaxFileSizeInput.Text, out int maxFileSizeKb))
            {
                SetControlValidationState(HarvestMaxFileSizeInput, true);
                ShowRequiredFieldsWarning("O campo 'Tamanho maximo por arquivo (KB)' deve ser numerico.");
                return;
            }

            if (!int.TryParse(HarvestMinScore.Text, out int minScore))
            {
                SetControlValidationState(HarvestMinScore, true);
                ShowRequiredFieldsWarning("O campo 'Score minimo' deve ser numerico.");
                return;
            }

            if (!int.TryParse(HarvestTopDefault.Text, out int topDefault))
            {
                SetControlValidationState(HarvestTopDefault, true);
                ShowRequiredFieldsWarning("O campo 'Top N default' deve ser numerico.");
                return;
            }

            if (!double.TryParse(HarvestWeightFanIn.Text, out double fanIn))
            {
                SetControlValidationState(HarvestWeightFanIn, true);
                ShowRequiredFieldsWarning("O campo 'Peso FanIn' deve ser numerico.");
                return;
            }

            if (!double.TryParse(HarvestWeightFanOut.Text, out double fanOut))
            {
                SetControlValidationState(HarvestWeightFanOut, true);
                ShowRequiredFieldsWarning("O campo 'Peso FanOut' deve ser numerico.");
                return;
            }

            if (!double.TryParse(HarvestWeightDensity.Text, out double density))
            {
                SetControlValidationState(HarvestWeightDensity, true);
                ShowRequiredFieldsWarning("O campo 'Peso Keyword Density' deve ser numerico.");
                return;
            }

            if (!double.TryParse(HarvestWeightDeadCode.Text, out double deadCode))
            {
                SetControlValidationState(HarvestWeightDeadCode, true);
                ShowRequiredFieldsWarning("O campo 'Peso DeadCode' deve ser numerico.");
                return;
            }

            // Parse Lists
            _currentHarvestConfig.Rules.Extensions = HarvestExtensions.Text
                .Split(',')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            _currentHarvestConfig.Rules.ExcludeDirectories = HarvestExcludeDirs.Text
                .Split(',')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            if (_currentHarvestConfig.Rules.Extensions.Count == 0)
            {
                ShowRequiredFieldsWarning("Informe ao menos uma extensao valida em 'Extensoes permitidas'.");
                return;
            }

            if (_currentHarvestConfig.Rules.ExcludeDirectories.Count == 0)
            {
                ShowRequiredFieldsWarning("Informe ao menos uma pasta valida em 'Pastas excluidas'.");
                return;
            }

            _currentHarvestConfig.Rules.MaxFileSizeKb = maxFileSizeKb;

            // Parse Limits
            _currentHarvestConfig.MinScoreDefault = minScore;
            _currentHarvestConfig.TopDefault = topDefault;

            // Parse Weights
            _currentHarvestConfig.Weights.FanInWeight = fanIn;
            _currentHarvestConfig.Weights.FanOutWeight = fanOut;
            _currentHarvestConfig.Weights.KeywordDensityWeight = density;
            _currentHarvestConfig.Weights.DeadCodePenalty = deadCode;

            _configService.SaveSection("Harvest", _currentHarvestConfig);
            UiMessageService.ShowInfo("Configuração do Harvest salva com sucesso!", "Sucesso");
            ShowMainStatusInfo("Configuração do Harvest salva com sucesso.");
        }
        catch (Exception ex)
        {
            UiMessageService.ShowError("Erro ao salvar configuracao do Harvest.", "Erro ao salvar", ex);
        }
    }

    // --- Organizer Settings ---

    private void OpenOrganizerSettings_Click(object sender, RoutedEventArgs e)
    {
        SettingsListPanel.Visibility = Visibility.Collapsed;
        OrganizerSettingsPanel.Visibility = Visibility.Visible;
        LoadOrganizerConfig();
    }

    private void LoadOrganizerConfig()
    {
        _currentOrganizerConfig = _configService.GetSection<OrganizerConfig>("Organizer");
        if (_currentOrganizerConfig.Categories == null) _currentOrganizerConfig.Categories = new();
        _currentOrganizerConfig.AllowedExtensions ??= Array.Empty<string>();
        if (_currentOrganizerConfig.AllowedExtensions.Length == 0)
            _currentOrganizerConfig.AllowedExtensions = new[] { ".pdf", ".txt", ".md", ".doc", ".docx" };
        _pendingNewOrganizerCategory = null;

        OrgAllowedExtensions.Text = string.Join(", ", _currentOrganizerConfig.AllowedExtensions);
        OrgMinScoreDefault.Text = _currentOrganizerConfig.MinScoreDefault.ToString();
        OrgFileNameWeight.Text = _currentOrganizerConfig.FileNameWeight.ToString("F1");
        OrgDedupByHash.IsChecked = _currentOrganizerConfig.DeduplicateByHash;
        OrgDedupByName.IsChecked = _currentOrganizerConfig.DeduplicateByName;
        OrgDedupFirstLines.Text = _currentOrganizerConfig.DeduplicateFirstLines.ToString();
        
        OrganizerCategoriesList.ItemsSource = null;
        OrganizerCategoriesList.ItemsSource = _currentOrganizerConfig.Categories;
        OrganizerCategoriesList.SelectedItem = null;
        OrganizerEditForm.Visibility = Visibility.Collapsed;
        UpdateOrganizerActionButtonsState();
    }

    private void AddOrganizerCategory_Click(object sender, RoutedEventArgs e)
    {
        var newCat = new OrganizerCategory(GenerateNextCategoryName(_currentOrganizerConfig.Categories), "NovaPasta", Array.Empty<string>());
        _currentOrganizerConfig.Categories.Add(newCat);
        _pendingNewOrganizerCategory = newCat;
        OrganizerCategoriesList.ItemsSource = null;
        OrganizerCategoriesList.ItemsSource = _currentOrganizerConfig.Categories;
        OrganizerCategoriesList.SelectedItem = newCat;
        UpdateOrganizerActionButtonsState();
    }

    private void OrganizerCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (OrganizerCategoriesList.SelectedItem is OrganizerCategory cat)
        {
            _selectedCategory = cat;
            OrgCatName.Text = cat.Name;
            OrgCatFolder.Text = cat.Folder;
            OrgCatKeywords.Text = string.Join(", ", cat.Keywords ?? Array.Empty<string>());
            OrganizerEditForm.Visibility = Visibility.Visible;
        }
        else
        {
            _selectedCategory = null;
            OrganizerEditForm.Visibility = Visibility.Collapsed;
        }

        UpdateOrganizerActionButtonsState();
    }

    private void SaveOrganizerCategory_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedCategory == null) return;

        var allowedExtensionsMissing = string.IsNullOrWhiteSpace(OrgAllowedExtensions.Text);
        var minScoreDefaultMissing = string.IsNullOrWhiteSpace(OrgMinScoreDefault.Text);
        var fileNameWeightMissing = string.IsNullOrWhiteSpace(OrgFileNameWeight.Text);
        var dedupFirstLinesMissing = string.IsNullOrWhiteSpace(OrgDedupFirstLines.Text);
        var nameMissing = string.IsNullOrWhiteSpace(OrgCatName.Text);
        var folderMissing = string.IsNullOrWhiteSpace(OrgCatFolder.Text);
        var keywordsMissing = string.IsNullOrWhiteSpace(OrgCatKeywords.Text);

        SetControlValidationState(OrgAllowedExtensions, allowedExtensionsMissing);
        SetControlValidationState(OrgMinScoreDefault, minScoreDefaultMissing);
        SetControlValidationState(OrgFileNameWeight, fileNameWeightMissing);
        SetControlValidationState(OrgDedupFirstLines, dedupFirstLinesMissing);
        SetControlValidationState(OrgCatName, nameMissing);
        SetControlValidationState(OrgCatFolder, folderMissing);
        SetControlValidationState(OrgCatKeywords, keywordsMissing);

        var missingFields = new List<string>();
        if (allowedExtensionsMissing)
            missingFields.Add("Extensões Permitidas");
        if (minScoreDefaultMissing)
            missingFields.Add("Score Mínimo Padrão");
        if (fileNameWeightMissing)
            missingFields.Add("Peso do Nome do Arquivo");
        if (dedupFirstLinesMissing)
            missingFields.Add("Deduplicar por Primeiras Linhas");
        if (nameMissing)
            missingFields.Add("Nome da Categoria");
        if (folderMissing)
            missingFields.Add("Nome da Pasta Destino");
        if (keywordsMissing)
            missingFields.Add("Palavras-Chave");

        if (!TryBuildRequiredFieldsMessage(missingFields, out var requiredMessage))
        {
            ShowRequiredFieldsWarning(requiredMessage);
            return;
        }

        if (!int.TryParse(OrgMinScoreDefault.Text, out var minScoreDefault))
        {
            SetControlValidationState(OrgMinScoreDefault, true);
            ShowRequiredFieldsWarning("Score Mínimo Padrão deve ser numérico.");
            return;
        }

        if (!double.TryParse(OrgFileNameWeight.Text, out var fileNameWeight))
        {
            SetControlValidationState(OrgFileNameWeight, true);
            ShowRequiredFieldsWarning("Peso do Nome do Arquivo deve ser numérico.");
            return;
        }

        if (!int.TryParse(OrgDedupFirstLines.Text, out var dedupFirstLines) || dedupFirstLines < 0)
        {
            SetControlValidationState(OrgDedupFirstLines, true);
            ShowRequiredFieldsWarning("Deduplicar por Primeiras Linhas deve ser numérico e maior ou igual a zero.");
            return;
        }

        var keywords = OrgCatKeywords.Text
            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToArray();

        if (keywords.Length == 0)
        {
            ShowRequiredFieldsWarning("Informe ao menos uma palavra-chave valida.");
            return;
        }

        var allowedExtensions = OrgAllowedExtensions.Text
            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.StartsWith(".") ? s : $".{s}")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (allowedExtensions.Length == 0)
        {
            SetControlValidationState(OrgAllowedExtensions, true);
            ShowRequiredFieldsWarning("Informe ao menos uma extensão válida em 'Extensões Permitidas'.");
            return;
        }

        _currentOrganizerConfig.AllowedExtensions = allowedExtensions;
        _currentOrganizerConfig.MinScoreDefault = minScoreDefault;
        _currentOrganizerConfig.FileNameWeight = fileNameWeight;
        _currentOrganizerConfig.DeduplicateByHash = OrgDedupByHash.IsChecked == true;
        _currentOrganizerConfig.DeduplicateByName = OrgDedupByName.IsChecked == true;
        _currentOrganizerConfig.DeduplicateFirstLines = dedupFirstLines;

        _selectedCategory.Name = OrgCatName.Text;
        _selectedCategory.Folder = OrgCatFolder.Text;
        _selectedCategory.Keywords = keywords;
        if (ReferenceEquals(_selectedCategory, _pendingNewOrganizerCategory))
        {
            _pendingNewOrganizerCategory = null;
        }

        _configService.SaveSection("Organizer", _currentOrganizerConfig);
        OrganizerCategoriesList.Items.Refresh();
        UiMessageService.ShowInfo("Categoria salva!", "Sucesso");
        ShowMainStatusInfo("Categoria salva com sucesso.");
        UpdateOrganizerActionButtonsState();
    }

    private void DeleteOrganizerCategory_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedCategory == null) return;
        if (ReferenceEquals(_selectedCategory, _pendingNewOrganizerCategory)) return;
        
        if (UiMessageService.Confirm($"Excluir categoria '{_selectedCategory.Name}'?", "Confirmar"))
        {
            if (ReferenceEquals(_selectedCategory, _pendingNewOrganizerCategory))
            {
                _pendingNewOrganizerCategory = null;
            }
            _currentOrganizerConfig.Categories.Remove(_selectedCategory);
            _configService.SaveSection("Organizer", _currentOrganizerConfig);
            LoadOrganizerConfig();
        }

        UpdateOrganizerActionButtonsState();
    }

    private void UpdateToolConfigurationActionButtonsState()
    {
        var hasSelection = _selectedToolConfiguration != null && ToolConfigurationEditForm.Visibility == Visibility.Visible;
        var isEditingPendingNew = hasSelection && _selectedToolConfiguration != null && _pendingNewToolConfigurations.Contains(_selectedToolConfiguration);
        var canDelete = hasSelection && !isEditingPendingNew;

        if (ToolConfigurationsActionBar != null)
        {
            ToolConfigurationsActionBar.ShowNew = true;
            ToolConfigurationsActionBar.ShowSave = hasSelection;
            ToolConfigurationsActionBar.ShowDelete = hasSelection;
            ToolConfigurationsActionBar.ShowCancel = true;
            ToolConfigurationsActionBar.CanNew = true;
            ToolConfigurationsActionBar.CanSave = hasSelection;
            ToolConfigurationsActionBar.CanDelete = canDelete;
            ToolConfigurationsActionBar.CanCancel = true;
        }

        UpdateToolConfigurationDefaultControlState();
    }

    private void UpdateOrganizerActionButtonsState()
    {
        var hasSelection = _selectedCategory != null && OrganizerEditForm.Visibility == Visibility.Visible;
        var isEditingPendingNew = hasSelection && ReferenceEquals(_selectedCategory, _pendingNewOrganizerCategory);
        var canDelete = hasSelection && !isEditingPendingNew;

        if (DeleteOrganizerCategoryButton != null)
        {
            DeleteOrganizerCategoryButton.Visibility = hasSelection ? Visibility.Visible : Visibility.Collapsed;
            DeleteOrganizerCategoryButton.IsEnabled = canDelete;
        }

        if (SaveOrganizerCategoryButton != null)
        {
            SaveOrganizerCategoryButton.Visibility = hasSelection ? Visibility.Visible : Visibility.Collapsed;
        }

        if (OrganizerAddCategoryButton != null)
        {
            OrganizerAddCategoryButton.IsEnabled = !isEditingPendingNew;
        }
    }

    private bool HasOtherDefaultConfiguration()
    {
        var list = (ToolConfigurationsList.ItemsSource as List<ToolConfiguration>) ?? new List<ToolConfiguration>();
        return list.Any(p => !ReferenceEquals(p, _selectedToolConfiguration) && p.IsDefault);
    }

    private static string GenerateNextConfigurationName(IReadOnlyCollection<ToolConfiguration> configurations, string? toolKey)
    {
        string prefix;
        if (IsProjectConfigurationMode(toolKey))
        {
            prefix = "Projeto";
        }
        else if (IsSshConnectionMode(toolKey))
        {
            prefix = "Tunel";
        }
        else if (IsNgrokConnectionMode(toolKey))
        {
            prefix = "Conexao";
        }
        else
        {
            prefix = "Configuracao";
        }

        var maxNumber = 0;

        foreach (var configuration in configurations)
        {
            if (configuration?.Name is null)
            {
                continue;
            }

            var trimmed = configuration.Name.Trim();
            var matchesPrefix = trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
            if (!matchesPrefix)
            {
                continue;
            }

            var suffix = trimmed.Substring(prefix.Length).Trim();
            if (int.TryParse(suffix, out var number) && number > maxNumber)
            {
                maxNumber = number;
            }
        }

        return $"{prefix}{maxNumber + 1}";
    }

    private static void NormalizeConfigurationDefaultsInMemory(List<ToolConfiguration> configurations)
    {
        if (configurations.Count == 0)
            return;

        var defaults = configurations.Where(p => p.IsDefault).ToList();
        if (defaults.Count == 1)
            return;

        foreach (var configuration in configurations)
            configuration.IsDefault = false;

        configurations[0].IsDefault = true;
    }

    private void SelectAndDisplayToolConfiguration(ToolConfiguration configuration)
    {
        ToolConfigurationsList.SelectedItem = configuration;
        ToolConfigurationsList.ScrollIntoView(configuration);
        DisplayToolConfiguration(configuration);
    }

    private void DisplayToolConfiguration(ToolConfiguration configuration)
    {
        _selectedToolConfiguration = configuration;
        ToolConfigurationNameInput.Text = configuration.Name;
        ToolConfigurationIsDefaultCheck.IsChecked = configuration.IsDefault;
        _toolConfigurationUIService.GenerateUIForConfiguration(_currentToolForConfigurations!, ToolConfigurationFieldsContainer, configuration);
        ToolConfigurationEditForm.Visibility = Visibility.Visible;
        UpdateToolConfigurationDefaultControlState();
    }

    private bool ShouldForceSingleConfigurationAsDefault()
    {
        var list = (ToolConfigurationsList.ItemsSource as List<ToolConfiguration>) ?? new List<ToolConfiguration>();
        return list.Count <= 1;
    }

    private void UpdateToolConfigurationDefaultControlState()
    {
        if (ToolConfigurationIsDefaultCheck == null || _selectedToolConfiguration == null)
            return;

        if (ShouldForceSingleConfigurationAsDefault())
        {
            ToolConfigurationIsDefaultCheck.IsChecked = true;
            ToolConfigurationIsDefaultCheck.IsEnabled = false;
            return;
        }

        ToolConfigurationIsDefaultCheck.IsEnabled = true;
    }

    private string GetToolConfigurationEntityNameLower()
    {
        if (IsProjectConfigurationMode())
            return "projeto";
        if (IsSshConnectionMode(_currentToolForConfigurations))
            return "túnel";
        if (IsNgrokConnectionMode(_currentToolForConfigurations))
            return "conexão";
        return "configuracao";
    }

    private string GetToolConfigurationSuccessMessage()
    {
        if (IsProjectConfigurationMode())
            return "Projeto salvo com sucesso!";
        if (IsSshConnectionMode(_currentToolForConfigurations))
            return "Túnel salvo com sucesso!";
        if (IsNgrokConnectionMode(_currentToolForConfigurations))
            return "Conexão salva com sucesso!";
        return "Configuração salva com sucesso.";
    }

    private static string GenerateNextCategoryName(IReadOnlyCollection<OrganizerCategory> categories)
    {
        var maxNumber = 0;

        foreach (var category in categories)
        {
            if (string.IsNullOrWhiteSpace(category?.Name))
            {
                continue;
            }

            var trimmed = category.Name.Trim();
            if (!trimmed.StartsWith("Categoria", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var suffix = trimmed.Substring("Categoria".Length).Trim();
            if (int.TryParse(suffix, out var number) && number > maxNumber)
            {
                maxNumber = number;
            }
        }

        return $"Categoria{maxNumber + 1}";
    }



    // --- Migrations Settings ---

    private void OpenMigrationsSettings_Click(object sender, RoutedEventArgs e)
    {
        SettingsListPanel.Visibility = Visibility.Collapsed;
        MigrationsSettingsPanel.Visibility = Visibility.Visible;
        LoadMigrationsConfig();
    }

    private void LoadMigrationsConfig()
    {
        _currentMigrationsConfig = _configService.GetSection<MigrationsSettings>("Migrations");
        NormalizeMigrationsSettings(_currentMigrationsConfig);
        
        MigRootPathSelector.SelectedPath = _currentMigrationsConfig.RootPath;
        MigStartupPathSelector.SelectedPath = _currentMigrationsConfig.StartupProjectPath;
        MigSqlServerTargetSelector.SelectedPath = GetMigrationsTargetPath(_currentMigrationsConfig, DatabaseProvider.SqlServer);
        MigSqliteTargetSelector.SelectedPath = GetMigrationsTargetPath(_currentMigrationsConfig, DatabaseProvider.Sqlite);
        MigContextInput.Text = _currentMigrationsConfig.DbContextFullName;
        MigArgsInput.Text = _currentMigrationsConfig.AdditionalArgs;
    }

    private void SaveMigrationsSettings_Click(object sender, RoutedEventArgs e)
    {
        var rootMissing = string.IsNullOrWhiteSpace(MigRootPathSelector.SelectedPath);
        var startupMissing = string.IsNullOrWhiteSpace(MigStartupPathSelector.SelectedPath);
        var sqlServerMissing = string.IsNullOrWhiteSpace(MigSqlServerTargetSelector.SelectedPath);
        var sqliteMissing = string.IsNullOrWhiteSpace(MigSqliteTargetSelector.SelectedPath);
        var contextMissing = string.IsNullOrWhiteSpace(MigContextInput.Text);

        SetPathSelectorValidationState(MigRootPathSelector, rootMissing);
        SetPathSelectorValidationState(MigStartupPathSelector, startupMissing);
        SetPathSelectorValidationState(MigSqlServerTargetSelector, sqlServerMissing);
        SetPathSelectorValidationState(MigSqliteTargetSelector, sqliteMissing);
        SetControlValidationState(MigContextInput, contextMissing);

        var missingFields = new List<string>();
        if (rootMissing)
            missingFields.Add("Caminho Raiz do Projeto (Root Path)");
        if (startupMissing)
            missingFields.Add("Caminho do Projeto de Startup");
        if (sqlServerMissing)
            missingFields.Add("Projeto de Migrations (SQL Server)");
        if (sqliteMissing)
            missingFields.Add("Projeto de Migrations (SQLite)");
        if (contextMissing)
            missingFields.Add("Nome Completo do DbContext");

        if (!TryBuildRequiredFieldsMessage(missingFields, out var requiredMessage))
        {
            ShowRequiredFieldsWarning(requiredMessage);
            return;
        }

        if (!TryValidateMigrationsAdditionalArgs(MigArgsInput.Text, out var additionalArgsError))
        {
            ShowRequiredFieldsWarning(additionalArgsError);
            return;
        }

        NormalizeMigrationsSettings(_currentMigrationsConfig);
        _currentMigrationsConfig.RootPath = MigRootPathSelector.SelectedPath;
        _currentMigrationsConfig.StartupProjectPath = MigStartupPathSelector.SelectedPath;
        SetMigrationsTargetPath(_currentMigrationsConfig, DatabaseProvider.SqlServer, MigSqlServerTargetSelector.SelectedPath);
        SetMigrationsTargetPath(_currentMigrationsConfig, DatabaseProvider.Sqlite, MigSqliteTargetSelector.SelectedPath);
        _currentMigrationsConfig.DbContextFullName = MigContextInput.Text.Trim();
        _currentMigrationsConfig.AdditionalArgs = string.IsNullOrWhiteSpace(MigArgsInput.Text)
            ? null
            : MigArgsInput.Text.Trim();

        _configService.SaveSection("Migrations", _currentMigrationsConfig);
        UiMessageService.ShowInfo("Configuracoes do Migrations salvas!", "Sucesso");
        ShowMainStatusInfo("Configuracoes do Migrations salvas.");
    }

    private void MigArgChip_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button || button.Content is not string argToken || string.IsNullOrWhiteSpace(argToken))
            return;

        if (ContainsMigrationsArgToken(MigArgsInput.Text, argToken))
        {
            ShowMainStatusInfo($"Argumento já adicionado: {argToken}");
            return;
        }

        var current = (MigArgsInput.Text ?? string.Empty).Trim();
        MigArgsInput.Text = string.IsNullOrWhiteSpace(current) ? argToken : $"{current} {argToken}";
        MigArgsInput.Focus();
        MigArgsInput.CaretIndex = MigArgsInput.Text.Length;
    }

    private static bool TryValidateMigrationsAdditionalArgs(string? args, out string error)
    {
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(args))
            return true;

        var blocked = BlockedMigrationsArgs.Where(token => ContainsMigrationsArgToken(args, token)).Distinct().ToList();
        if (blocked.Count == 0)
            return true;

        error = "Os argumentos abaixo não podem ser usados em 'Argumentos Adicionais' para Migrations:\n- "
            + string.Join("\n- ", blocked)
            + "\n\nUse os campos próprios da tela para Projeto/Startup/DbContext.";
        return false;
    }

    private static bool ContainsMigrationsArgToken(string? args, string token)
    {
        if (string.IsNullOrWhiteSpace(args) || string.IsNullOrWhiteSpace(token))
            return false;

        var pattern = $@"(?<!\S){Regex.Escape(token)}(?=\s|$|=)";
        return Regex.IsMatch(args, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static void NormalizeMigrationsSettings(MigrationsSettings settings)
    {
        settings.Targets ??= new List<MigrationTarget>();
        EnsureMigrationsTarget(settings, DatabaseProvider.SqlServer);
        EnsureMigrationsTarget(settings, DatabaseProvider.Sqlite);
    }

    private static MigrationTarget EnsureMigrationsTarget(MigrationsSettings settings, DatabaseProvider provider)
    {
        var target = settings.Targets.FirstOrDefault(item => item.Provider == provider);
        if (target is not null)
            return target;

        target = new MigrationTarget
        {
            Provider = provider,
            MigrationsProjectPath = string.Empty
        };
        settings.Targets.Add(target);
        return target;
    }

    private static string GetMigrationsTargetPath(MigrationsSettings settings, DatabaseProvider provider)
    {
        return EnsureMigrationsTarget(settings, provider).MigrationsProjectPath;
    }

    private static void SetMigrationsTargetPath(MigrationsSettings settings, DatabaseProvider provider, string path)
    {
        EnsureMigrationsTarget(settings, provider).MigrationsProjectPath = path ?? string.Empty;
    }

    // --- Ngrok Settings ---

    private void OpenNgrokSettings_Click(object sender, RoutedEventArgs e)
    {
        SettingsListPanel.Visibility = Visibility.Collapsed;
        NgrokSettingsPanel.Visibility = Visibility.Visible;
        LoadNgrokConfig();
    }

    private void LoadNgrokConfig()
    {
        _currentNgrokConfig = _ngrokSetupService.GetSettings();
        _currentNgrokConfig.Normalize();

        NgrokExeSelector.SelectedPath = _currentNgrokConfig.ExecutablePath;
        NgrokAuthTokenInput.Text = _currentNgrokConfig.AuthToken;
        NgrokArgsInput.Text = _currentNgrokConfig.AdditionalArgs;
    }

    private void SaveNgrokSettings_Click(object sender, RoutedEventArgs e)
    {
        var authTokenMissing = string.IsNullOrWhiteSpace(NgrokAuthTokenInput.Text);
        SetControlValidationState(NgrokAuthTokenInput, authTokenMissing);

        var missingFields = new List<string>();
        if (authTokenMissing)
            missingFields.Add("Auth Token");

        if (!TryBuildRequiredFieldsMessage(missingFields, out var requiredMessage))
        {
            ShowRequiredFieldsWarning(requiredMessage);
            return;
        }

        _currentNgrokConfig.ExecutablePath = NgrokExeSelector.SelectedPath;
        _currentNgrokConfig.AuthToken = NgrokAuthTokenInput.Text.Trim();
        _currentNgrokConfig.AdditionalArgs = NgrokArgsInput.Text.Trim();

        _ngrokSetupService.SaveSettings(_currentNgrokConfig);
        UiMessageService.ShowInfo("Configuracoes do Ngrok salvas!", "Sucesso");
        ShowMainStatusInfo("Configuracoes do Ngrok salvas.");
    }

    // --- Notes and Cloud Settings ---

    private void OpenNotesCloudSettings_Click(object sender, RoutedEventArgs e)
    {
        SettingsListPanel.Visibility = Visibility.Collapsed;
        NotesCloudSettingsPanel.Visibility = Visibility.Visible;
        LoadNotesCloudConfig();
    }

    private void LoadNotesCloudConfig()
    {
        _currentNotesSettings = _configService.GetSection<NotesSettings>("Notes") ?? new();
        _currentGoogleDriveSettings = _configService.GetSection<GoogleDriveSettings>("GoogleDrive") ?? new();

        // Local Storage
        _currentNotesSettings.StoragePath = ResolveNotesStoragePath(_currentNotesSettings.StoragePath);
        _currentNotesSettings.InitialListDisplay = NormalizeNotesInitialListDisplay(_currentNotesSettings.InitialListDisplay);
        NotesStoragePathSelector.SelectedPath = _currentNotesSettings.StoragePath;
        NotesAutoCloudSyncCheck.IsChecked = _currentNotesSettings.AutoCloudSync;
        
        // Selecionar formato no combo
        foreach (ComboBoxItem item in NotesFormatCombo.Items)
        {
            if (item.Content.ToString() == _currentNotesSettings.DefaultFormat)
            {
                NotesFormatCombo.SelectedItem = item;
                break;
            }
        }

        if (NotesFormatCombo.SelectedItem == null && NotesFormatCombo.Items.Count > 0)
        {
            NotesFormatCombo.SelectedIndex = 0;
        }

        foreach (ComboBoxItem item in NotesInitialListDisplayCombo.Items)
        {
            if (item.Content?.ToString() == _currentNotesSettings.InitialListDisplay)
            {
                NotesInitialListDisplayCombo.SelectedItem = item;
                break;
            }
        }

        if (NotesInitialListDisplayCombo.SelectedItem == null && NotesInitialListDisplayCombo.Items.Count > 0)
        {
            NotesInitialListDisplayCombo.SelectedIndex = 0;
        }

        // Google Drive
        var hasAnyGoogleDriveValue =
            !string.IsNullOrWhiteSpace(_currentGoogleDriveSettings.ClientId)
            || !string.IsNullOrWhiteSpace(_currentGoogleDriveSettings.ClientSecret)
            || !string.IsNullOrWhiteSpace(_currentGoogleDriveSettings.ProjectId)
            || !string.IsNullOrWhiteSpace(_currentGoogleDriveSettings.FolderName)
            || _currentGoogleDriveSettings.IsEnabled;

        var hasRequiredGoogleDriveCredentials =
            !string.IsNullOrWhiteSpace(_currentGoogleDriveSettings.ClientId)
            && !string.IsNullOrWhiteSpace(_currentGoogleDriveSettings.ClientSecret)
            && !string.IsNullOrWhiteSpace(_currentGoogleDriveSettings.ProjectId)
            && !string.IsNullOrWhiteSpace(_currentGoogleDriveSettings.FolderName);

        if (_currentGoogleDriveSettings.IsEnabled && !hasRequiredGoogleDriveCredentials)
        {
            // Protege primeira abertura/estado incompleto para nao aparecer marcado por engano.
            _currentGoogleDriveSettings.IsEnabled = false;
        }

        GDriveEnabledCheck.IsChecked = hasAnyGoogleDriveValue
            ? _currentGoogleDriveSettings.IsEnabled
            : false;
        GDriveClientId.Text = _currentGoogleDriveSettings.ClientId;
        GDriveClientSecret.Text = _currentGoogleDriveSettings.ClientSecret;
        GDriveProjectId.Text = _currentGoogleDriveSettings.ProjectId;
        GDriveFolderName.Text = string.IsNullOrEmpty(_currentGoogleDriveSettings.FolderName) ? "DevToolsNotes" : _currentGoogleDriveSettings.FolderName;
        
        // Atualiza estado visual do grid
        GDriveEnabledCheck_Changed(null!, null!);
    }

    private void SaveNotesCloudSettings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Local Storage
            _currentNotesSettings.StoragePath = ResolveNotesStoragePath(NotesStoragePathSelector.SelectedPath);
            NotesStoragePathSelector.SelectedPath = _currentNotesSettings.StoragePath;
            _currentNotesSettings.AutoCloudSync = NotesAutoCloudSyncCheck.IsChecked == true;
            _currentNotesSettings.DefaultFormat = (NotesFormatCombo.SelectedItem as ComboBoxItem)?.Content.ToString() ?? ".txt";
            _currentNotesSettings.InitialListDisplay = NormalizeNotesInitialListDisplay((NotesInitialListDisplayCombo.SelectedItem as ComboBoxItem)?.Content?.ToString());

            var notesStorageMissing = string.IsNullOrWhiteSpace(_currentNotesSettings.StoragePath);
            var defaultFormatMissing = NotesFormatCombo.SelectedItem == null;
            var initialDisplayMissing = NotesInitialListDisplayCombo.SelectedItem == null;

            SetPathSelectorValidationState(NotesStoragePathSelector, notesStorageMissing);
            SetControlValidationState(NotesFormatCombo, defaultFormatMissing);
            SetControlValidationState(NotesInitialListDisplayCombo, initialDisplayMissing);

            var missingFields = new List<string>();
            if (notesStorageMissing)
                missingFields.Add("Pasta de Armazenamento");
            if (defaultFormatMissing)
                missingFields.Add("Formato Padrao");
            if (initialDisplayMissing)
                missingFields.Add("Exibicao Inicial da Lista");

            if (!TryBuildRequiredFieldsMessage(missingFields, out var notesRequiredMessage))
            {
                ShowRequiredFieldsWarning(notesRequiredMessage);
                return;
            }

            // Google Drive
            _currentGoogleDriveSettings = ReadGoogleDriveSettingsFromUi();
            SetControlValidationState(GDriveClientId, false);
            SetControlValidationState(GDriveClientSecret, false);
            SetControlValidationState(GDriveProjectId, false);
            SetControlValidationState(GDriveFolderName, false);
            if (_currentGoogleDriveSettings.IsEnabled && !ValidateGoogleDriveSettings(_currentGoogleDriveSettings, out var validationMessage))
            {
                SetControlValidationState(GDriveClientId, string.IsNullOrWhiteSpace(_currentGoogleDriveSettings.ClientId));
                SetControlValidationState(GDriveClientSecret, string.IsNullOrWhiteSpace(_currentGoogleDriveSettings.ClientSecret));
                SetControlValidationState(GDriveProjectId, string.IsNullOrWhiteSpace(_currentGoogleDriveSettings.ProjectId));
                SetControlValidationState(GDriveFolderName, string.IsNullOrWhiteSpace(_currentGoogleDriveSettings.FolderName));
                ShowRequiredFieldsWarning(validationMessage);
                return;
            }

            _configService.SaveSection("Notes", _currentNotesSettings);
            _configService.SaveSection("GoogleDrive", _currentGoogleDriveSettings);

            UiMessageService.ShowInfo("Configuracoes de Notas e Nuvem salvas com sucesso!", "Sucesso");
            ShowMainStatusInfo("Configuracoes de Notas e Nuvem salvas.");
        }
        catch (Exception ex)
        {
            UiMessageService.ShowError("Erro ao salvar configurações de Notas.", "Erro ao salvar", ex);
        }
    }

    private void GDriveEnabledCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (GDriveConfigGrid != null && GDriveEnabledCheck != null)
        {
            GDriveConfigGrid.IsEnabled = GDriveEnabledCheck.IsChecked == true;
        }
    }

    private void OpenHelp_Click(object sender, RoutedEventArgs e)
    {
        var helpWindow = new HelpWindow();
        helpWindow.Owner = this;
        helpWindow.ShowDialog();
    }

    private async void TestConnection_Click(object sender, RoutedEventArgs e)
    {
        if (_isTestingGoogleDriveConnection)
            return;

        var tempSettings = ReadGoogleDriveSettingsFromUi();
        tempSettings.IsEnabled = true; // Teste deve funcionar mesmo com o toggle desligado.

        SetControlValidationState(GDriveClientId, false);
        SetControlValidationState(GDriveClientSecret, false);
        SetControlValidationState(GDriveProjectId, false);
        SetControlValidationState(GDriveFolderName, false);

        if (!ValidateGoogleDriveSettings(tempSettings, out var validationMessage))
        {
            SetControlValidationState(GDriveClientId, string.IsNullOrWhiteSpace(tempSettings.ClientId));
            SetControlValidationState(GDriveClientSecret, string.IsNullOrWhiteSpace(tempSettings.ClientSecret));
            SetControlValidationState(GDriveProjectId, string.IsNullOrWhiteSpace(tempSettings.ProjectId));
            SetControlValidationState(GDriveFolderName, string.IsNullOrWhiteSpace(tempSettings.FolderName));
            ShowRequiredFieldsWarning(validationMessage);
            return;
        }

        _isTestingGoogleDriveConnection = true;
        SetGDriveTestUiState(isTesting: true, statusText: "Testando conexao com Google Drive...");
        try
        {
            // Mantemos o fluxo assincrono sem bloquear a UI.
            await _googleDriveService.TestConnectionAsync(tempSettings);

            SetGDriveTestUiState(isTesting: false, statusText: "Conexao validada com sucesso.");
            UiMessageService.ShowInfo("Conexao com Google Drive estabelecida com sucesso!", "Sucesso");
            ShowMainStatusInfo("Conexao com Google Drive validada com sucesso.");
        }
        catch (Exception ex)
        {
            SetGDriveTestUiState(isTesting: false, statusText: "Falha ao validar conexao.", isError: true);
            UiMessageService.ShowError("Falha ao conectar com Google Drive. Verifique as credenciais.", "Erro de Conexao", ex);
        }
        finally
        {
            _isTestingGoogleDriveConnection = false;
            SetGDriveTestUiState(isTesting: false, statusText: GDriveTestConnectionStatus?.Text, isError: false, preserveStatus: true);
        }
    }

    private void ShowRequiredFieldsWarning(string message)
    {
        if (MainStatusText == null)
        {
            return;
        }

        MainStatusText.Text = message;
        MainStatusText.Foreground = (System.Windows.Media.Brush)FindResource("ErrorBrush");
    }

    private void SetControlValidationState(System.Windows.Controls.Control? control, bool invalid)
    {
        if (control == null)
            return;

        if (invalid)
        {
            control.BorderBrush = (System.Windows.Media.Brush)FindResource("ErrorBrush");
            control.BorderThickness = new Thickness(1.5);
            return;
        }

        control.ClearValue(System.Windows.Controls.Control.BorderBrushProperty);
        control.ClearValue(System.Windows.Controls.Control.BorderThicknessProperty);
    }

    private void SetPathSelectorValidationState(PathSelector? selector, bool invalid)
    {
        if (selector == null)
            return;

        var textBox = selector.FindName("PathInput") as System.Windows.Controls.TextBox;
        SetControlValidationState(textBox, invalid);
    }

    private void ShowMainStatusInfo(string message)
    {
        if (MainStatusText == null)
        {
            return;
        }

        MainStatusText.Text = message;
        MainStatusText.Foreground = (System.Windows.Media.Brush)FindResource("DevToolsTextSecondary");
    }

    private static bool TryBuildRequiredFieldsMessage(List<string> missing, out string message)
    {
        if (missing.Count == 0)
        {
            message = string.Empty;
            return true;
        }

        message = "Os campos abaixo não podem ficar em branco:\n- " + string.Join("\n- ", missing);
        return false;
    }

    private static string ResolveNotesStoragePath(string? candidatePath)
    {
        var resolved = string.IsNullOrWhiteSpace(candidatePath)
            ? NotesStorageDefaults.GetDefaultPath()
            : candidatePath.Trim();

        var fullPath = System.IO.Path.GetFullPath(resolved);
        System.IO.Directory.CreateDirectory(fullPath);
        return fullPath;
    }

    private static string NormalizeNotesInitialListDisplay(string? value)
    {
        return value switch
        {
            "8" => "8",
            "15" => "15",
            "20" => "20",
            _ => "Auto"
        };
    }

    private GoogleDriveSettings ReadGoogleDriveSettingsFromUi()
    {
        return new GoogleDriveSettings
        {
            IsEnabled = GDriveEnabledCheck.IsChecked == true,
            ClientId = (GDriveClientId.Text ?? string.Empty).Trim(),
            ClientSecret = (GDriveClientSecret.Text ?? string.Empty).Trim(),
            ProjectId = (GDriveProjectId.Text ?? string.Empty).Trim(),
            FolderName = (GDriveFolderName.Text ?? string.Empty).Trim()
        };
    }

    private static bool ValidateGoogleDriveSettings(GoogleDriveSettings settings, out string message)
    {
        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(settings.ClientId))
            missing.Add("Client ID");
        if (string.IsNullOrWhiteSpace(settings.ClientSecret))
            missing.Add("Client Secret");
        if (string.IsNullOrWhiteSpace(settings.ProjectId))
            missing.Add("Project ID");
        if (string.IsNullOrWhiteSpace(settings.FolderName))
            missing.Add("Nome da Pasta no Drive");

        if (missing.Count == 0)
        {
            message = string.Empty;
            return true;
        }

        message = "Os campos abaixo não podem ficar em branco:\n- " + string.Join("\n- ", missing);
        return false;
    }

    private void SetGDriveTestUiState(bool isTesting, string? statusText = null, bool isError = false, bool preserveStatus = false)
    {
        if (GDriveTestConnectionButton != null)
        {
            GDriveTestConnectionButton.IsEnabled = !isTesting;
            GDriveTestConnectionButton.Content = isTesting ? "Testando..." : "Testar Conexao";
        }

        if (GDriveTestConnectionStatus != null && !preserveStatus)
        {
            GDriveTestConnectionStatus.Text = statusText ?? string.Empty;
            GDriveTestConnectionStatus.Foreground = isError
                ? (System.Windows.Media.Brush)FindResource("ErrorBrush")
                : (System.Windows.Media.Brush)FindResource("SecondaryTextBrush");
        }
    }
}







