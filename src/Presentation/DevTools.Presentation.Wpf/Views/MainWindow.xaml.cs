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
using System.Collections.Generic;
using System.Linq;
using DevTools.Core.Models;
using System.Windows.Media;

namespace DevTools.Presentation.Wpf.Views;

public partial class MainWindow : Window
{
    private const string StorageBackendEnvVar = "DEVTOOLS_STORAGE_BACKEND";
    private static readonly List<string> DefaultHarvestExcludeDirectories = new()
    {
        "bin", "obj", ".git", ".vs", "node_modules",
        "dist", "build", ".idea", ".vscode", ".next", ".nuxt", ".turbo", "Snapshot"
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
    private string? _currentToolForProfiles;
    private ToolProfile? _selectedToolProfile;
    private readonly ProfileUIService _profileUIService;
    private bool _isTestingGoogleDriveConnection;
    private bool _allowCloseForShutdown;
    private string? _currentEmbeddedToolId;
    private bool _isSyncingOwnedWindows;
    private bool _hasTrackedMainLocation;
    private double _lastMainLeft;
    private double _lastMainTop;

    public MainWindow(TrayService trayService, JobManager jobManager, ConfigService configService, ProfileUIService profileUIService, GoogleDriveService googleDriveService)
    {
        InitializeComponent();
        _trayService = trayService;
        _jobManager = jobManager;
        _configService = configService;
        _profileUIService = profileUIService;
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
        _profileUIService = null!;
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
        ToolProfilesSettingsPanel.Visibility = Visibility.Collapsed;
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
        if (StorageBackendCombo.SelectedItem == null)
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

    // --- Generic Tool Profiles Management ---

    private void OpenToolProfiles_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.CommandParameter is string toolKey)
        {
            _currentToolForProfiles = toolKey;
            ToolProfilesTitle.Text = $"Perfis: {btn.Content}";
            SettingsListPanel.Visibility = Visibility.Collapsed;
            ToolProfilesSettingsPanel.Visibility = Visibility.Visible;
            LoadToolProfiles();
        }
    }

    private void LoadToolProfiles()
    {
        if (string.IsNullOrEmpty(_currentToolForProfiles)) return;

        var profiles = _profileUIService.LoadProfiles(_currentToolForProfiles);
        ToolProfilesList.ItemsSource = null;
        ToolProfilesList.ItemsSource = profiles;
        
        ToolProfileEditForm.Visibility = Visibility.Collapsed;
        ToolProfilesList.SelectedItem = null;
    }

    private void AddToolProfile_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_currentToolForProfiles)) return;

        var newProfile = new ToolProfile 
        { 
            Name = "Novo Perfil " + (ToolProfilesList.Items.Count + 1)
        };
        
        var list = (ToolProfilesList.ItemsSource as List<ToolProfile>) ?? new List<ToolProfile>();
        list.Add(newProfile);
        
        ToolProfilesList.ItemsSource = null;
        ToolProfilesList.ItemsSource = list;
        ToolProfilesList.SelectedItem = newProfile;
    }

    private void ToolProfile_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ToolProfilesList.SelectedItem is ToolProfile profile)
        {
            _selectedToolProfile = profile;
            ToolProfileNameInput.Text = profile.Name;
            ToolProfileIsDefaultCheck.IsChecked = profile.IsDefault;
            
            _profileUIService.GenerateUIForProfile(_currentToolForProfiles!, ToolProfileFieldsContainer, profile);
            ToolProfileEditForm.Visibility = Visibility.Visible;
        }
        else
        {
            _selectedToolProfile = null;
            ToolProfileEditForm.Visibility = Visibility.Collapsed;
        }
    }

    private void SaveToolProfile_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedToolProfile == null || string.IsNullOrEmpty(_currentToolForProfiles)) return;

        if (string.IsNullOrWhiteSpace(ToolProfileNameInput.Text))
        {
            ShowRequiredFieldsWarning("Os campos abaixo não podem ficar em branco:\n- Nome do Perfil");
            return;
        }

        _selectedToolProfile.Name = ToolProfileNameInput.Text.Trim();
        _selectedToolProfile.IsDefault = ToolProfileIsDefaultCheck.IsChecked == true;

        // Coletar valores dos campos dinamicos recursivamente
        CollectProfileOptions(ToolProfileFieldsContainer, _selectedToolProfile.Options);

        _profileUIService.SaveProfile(_currentToolForProfiles, _selectedToolProfile);
        ToolProfilesList.Items.Refresh();
        UiMessageService.ShowInfo("Perfil salvo com sucesso!", "Sucesso");
        ShowMainStatusInfo("Perfil salvo com sucesso.");
    }

    private void CollectProfileOptions(DependencyObject container, Dictionary<string, string> options)
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
            else if (child is DependencyObject depObj)
            {
                // Busca recursiva em containers (Grid, StackPanel, Card, etc.)
                CollectProfileOptions(depObj, options);
            }
        }
    }

    private void DeleteToolProfile_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedToolProfile == null || string.IsNullOrEmpty(_currentToolForProfiles)) return;

        if (UiMessageService.Confirm($"Excluir perfil '{_selectedToolProfile.Name}'?", "Confirmar"))
        {
            _profileUIService.DeleteProfile(_currentToolForProfiles, _selectedToolProfile.Name);
            LoadToolProfiles();
        }
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
            _currentHarvestConfig.Rules.ExcludeDirectories = new List<string>(DefaultHarvestExcludeDirectories);
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
            var missingFields = new List<string>();
            if (string.IsNullOrWhiteSpace(HarvestExtensions.Text))
                missingFields.Add("Extensoes permitidas");
            if (string.IsNullOrWhiteSpace(HarvestExcludeDirs.Text))
                missingFields.Add("Pastas excluidas");
            if (string.IsNullOrWhiteSpace(HarvestMaxFileSizeInput.Text))
                missingFields.Add("Tamanho maximo por arquivo (KB)");
            if (string.IsNullOrWhiteSpace(HarvestMinScore.Text))
                missingFields.Add("Score minimo");
            if (string.IsNullOrWhiteSpace(HarvestTopDefault.Text))
                missingFields.Add("Top N default");
            if (string.IsNullOrWhiteSpace(HarvestWeightFanIn.Text))
                missingFields.Add("Peso FanIn");
            if (string.IsNullOrWhiteSpace(HarvestWeightFanOut.Text))
                missingFields.Add("Peso FanOut");
            if (string.IsNullOrWhiteSpace(HarvestWeightDensity.Text))
                missingFields.Add("Peso Keyword Density");
            if (string.IsNullOrWhiteSpace(HarvestWeightDeadCode.Text))
                missingFields.Add("Peso DeadCode");

            if (!TryBuildRequiredFieldsMessage(missingFields, out var requiredMessage))
            {
                ShowRequiredFieldsWarning(requiredMessage);
                return;
            }

            if (!int.TryParse(HarvestMaxFileSizeInput.Text, out int maxFileSizeKb))
            {
                ShowRequiredFieldsWarning("O campo 'Tamanho maximo por arquivo (KB)' deve ser numerico.");
                return;
            }

            if (!int.TryParse(HarvestMinScore.Text, out int minScore))
            {
                ShowRequiredFieldsWarning("O campo 'Score minimo' deve ser numerico.");
                return;
            }

            if (!int.TryParse(HarvestTopDefault.Text, out int topDefault))
            {
                ShowRequiredFieldsWarning("O campo 'Top N default' deve ser numerico.");
                return;
            }

            if (!double.TryParse(HarvestWeightFanIn.Text, out double fanIn))
            {
                ShowRequiredFieldsWarning("O campo 'Peso FanIn' deve ser numerico.");
                return;
            }

            if (!double.TryParse(HarvestWeightFanOut.Text, out double fanOut))
            {
                ShowRequiredFieldsWarning("O campo 'Peso FanOut' deve ser numerico.");
                return;
            }

            if (!double.TryParse(HarvestWeightDensity.Text, out double density))
            {
                ShowRequiredFieldsWarning("O campo 'Peso Keyword Density' deve ser numerico.");
                return;
            }

            if (!double.TryParse(HarvestWeightDeadCode.Text, out double deadCode))
            {
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
        
        OrganizerCategoriesList.ItemsSource = null;
        OrganizerCategoriesList.ItemsSource = _currentOrganizerConfig.Categories;
        OrganizerCategoriesList.SelectedItem = null;
        OrganizerEditForm.Visibility = Visibility.Collapsed;
    }

    private void AddOrganizerCategory_Click(object sender, RoutedEventArgs e)
    {
        var newCat = new OrganizerCategory("Nova Categoria", "NovaPasta", Array.Empty<string>());
        _currentOrganizerConfig.Categories.Add(newCat);
        OrganizerCategoriesList.ItemsSource = null;
        OrganizerCategoriesList.ItemsSource = _currentOrganizerConfig.Categories;
        OrganizerCategoriesList.SelectedItem = newCat;
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
    }

    private void SaveOrganizerCategory_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedCategory == null) return;

        var missingFields = new List<string>();
        if (string.IsNullOrWhiteSpace(OrgCatName.Text))
            missingFields.Add("Nome da Categoria");
        if (string.IsNullOrWhiteSpace(OrgCatFolder.Text))
            missingFields.Add("Nome da Pasta Destino");
        if (string.IsNullOrWhiteSpace(OrgCatKeywords.Text))
            missingFields.Add("Palavras-Chave");

        if (!TryBuildRequiredFieldsMessage(missingFields, out var requiredMessage))
        {
            ShowRequiredFieldsWarning(requiredMessage);
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

        _selectedCategory.Name = OrgCatName.Text;
        _selectedCategory.Folder = OrgCatFolder.Text;
        _selectedCategory.Keywords = keywords;

        _configService.SaveSection("Organizer", _currentOrganizerConfig);
        OrganizerCategoriesList.Items.Refresh();
        UiMessageService.ShowInfo("Categoria salva!", "Sucesso");
        ShowMainStatusInfo("Categoria salva com sucesso.");
    }

    private void DeleteOrganizerCategory_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedCategory == null) return;
        
        if (UiMessageService.Confirm($"Excluir categoria '{_selectedCategory.Name}'?", "Confirmar"))
        {
            _currentOrganizerConfig.Categories.Remove(_selectedCategory);
            _configService.SaveSection("Organizer", _currentOrganizerConfig);
            LoadOrganizerConfig();
        }
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
        
        MigRootPathSelector.SelectedPath = _currentMigrationsConfig.RootPath;
        MigStartupPathSelector.SelectedPath = _currentMigrationsConfig.StartupProjectPath;
        MigContextInput.Text = _currentMigrationsConfig.DbContextFullName;
        MigArgsInput.Text = _currentMigrationsConfig.AdditionalArgs;
    }

    private void SaveMigrationsSettings_Click(object sender, RoutedEventArgs e)
    {
        var missingFields = new List<string>();
        if (string.IsNullOrWhiteSpace(MigRootPathSelector.SelectedPath))
            missingFields.Add("Caminho Raiz do Projeto (Root Path)");
        if (string.IsNullOrWhiteSpace(MigStartupPathSelector.SelectedPath))
            missingFields.Add("Caminho do Projeto de Startup");
        if (string.IsNullOrWhiteSpace(MigContextInput.Text))
            missingFields.Add("Nome Completo do DbContext");
        if (string.IsNullOrWhiteSpace(MigArgsInput.Text))
            missingFields.Add("Argumentos Adicionais");

        if (!TryBuildRequiredFieldsMessage(missingFields, out var requiredMessage))
        {
            ShowRequiredFieldsWarning(requiredMessage);
            return;
        }

        _currentMigrationsConfig.RootPath = MigRootPathSelector.SelectedPath;
        _currentMigrationsConfig.StartupProjectPath = MigStartupPathSelector.SelectedPath;
        _currentMigrationsConfig.DbContextFullName = MigContextInput.Text.Trim();
        _currentMigrationsConfig.AdditionalArgs = MigArgsInput.Text.Trim();

        _configService.SaveSection("Migrations", _currentMigrationsConfig);
        UiMessageService.ShowInfo("Configuracoes do Migrations salvas!", "Sucesso");
        ShowMainStatusInfo("Configuracoes do Migrations salvas.");
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
        var missingFields = new List<string>();
        if (string.IsNullOrWhiteSpace(NgrokAuthTokenInput.Text))
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
        GDriveEnabledCheck.IsChecked = _currentGoogleDriveSettings.IsEnabled;
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

            var missingFields = new List<string>();
            if (string.IsNullOrWhiteSpace(_currentNotesSettings.StoragePath))
                missingFields.Add("Pasta de Armazenamento");
            if (NotesFormatCombo.SelectedItem == null)
                missingFields.Add("Formato Padrao");
            if (NotesInitialListDisplayCombo.SelectedItem == null)
                missingFields.Add("Exibicao Inicial da Lista");

            if (!TryBuildRequiredFieldsMessage(missingFields, out var notesRequiredMessage))
            {
                ShowRequiredFieldsWarning(notesRequiredMessage);
                return;
            }

            // Google Drive
            _currentGoogleDriveSettings = ReadGoogleDriveSettingsFromUi();
            if (_currentGoogleDriveSettings.IsEnabled && !ValidateGoogleDriveSettings(_currentGoogleDriveSettings, out var validationMessage))
            {
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

        if (!ValidateGoogleDriveSettings(tempSettings, out var validationMessage))
        {
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




