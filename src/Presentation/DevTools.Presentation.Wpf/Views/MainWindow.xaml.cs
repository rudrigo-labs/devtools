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
using DevTools.Ngrok.Models;
using DevTools.Presentation.Wpf.Models;
using System.Collections.Generic;
using System.Linq;
using DevTools.Core.Models;
using System.Windows.Media;

namespace DevTools.Presentation.Wpf.Views;

public partial class MainWindow : Window
{
    private readonly TrayService _trayService;
    private readonly JobManager _jobManager;
    private readonly ConfigService _configService;
    private readonly GoogleDriveService _googleDriveService;

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

    public MainWindow(TrayService trayService, JobManager jobManager, ConfigService configService, ProfileUIService profileUIService, GoogleDriveService googleDriveService)
    {
        InitializeComponent();
        _trayService = trayService;
        _jobManager = jobManager;
        _configService = configService;
        _profileUIService = profileUIService;
        _googleDriveService = googleDriveService;

        _trayService.SetMainWindow(this);
        _trayService.EmbeddedToolRequested += TrayService_EmbeddedToolRequested;

        // Binding direto da coleção de Jobs
        JobsDataGrid.ItemsSource = _jobManager.Jobs;

        this.Loaded += MainWindow_Loaded;
        this.Closing += MainWindow_Closing;
        this.IsVisibleChanged += MainWindow_IsVisibleChanged;
        this.StateChanged += MainWindow_StateChanged;
    }

    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        // Se a principal minimizar, a ferramenta aberta deve acompanhar (se houver)
        if (_trayService.HasOpenToolWindow)
        {
            // O WPF já lida com Owner minimizando junto se WindowState mudar
            // mas garantimos que a lógica de Hide/Show funcione
        }
    }

    private void MainWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        // Se a MainWindow for escondida (Hide), temos que esconder a ferramenta atual também
        // para que ela não fique "orfã" na tela
        if (this.Visibility != Visibility.Visible)
        {
            _trayService.OpenTool("HIDE_CURRENT"); // Comando interno para esconder se necessário
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
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Posiciona no canto inferior direito (WorkArea)
        var workArea = SystemParameters.WorkArea;
        this.Left = workArea.Right - this.Width - 20;
        this.Top = workArea.Bottom - this.Height - 20;
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
            Text = $"A ferramenta '{title}' nao possui conteudo embutido configurado.",
            Foreground = (System.Windows.Media.Brush)FindResource("DevToolsTextSecondary"),
            Margin = new Thickness(24),
            FontSize = 14,
            TextWrapping = TextWrapping.Wrap
        };
    }

    /// <summary>
    /// Botão Encerrar escolha:Sim
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
        MaterialDesignThemes.Wpf.DialogHost.Show(RootDialog.DialogContent, "RootDialog");
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        this.WindowState = WindowState.Minimized;
    }

    private void MinimizeToTray_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    private void MinimizeToTrayFromDialog_Click(object sender, RoutedEventArgs e)
    {
        MaterialDesignThemes.Wpf.DialogHost.Close("RootDialog");
        Hide();
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            this.DragMove();
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

        // Se o usuário clicar no X ou Alt+F4, mostramos o diálogo em vez de esconder
        e.Cancel = true;
        CloseButton_Click(this, new RoutedEventArgs());
    }

    private string BuildCloseDialogMessage()
    {
        var details = new List<string>();
        if (_jobManager.RunningJobsCount > 0)
        {
            details.Add($"- Jobs em execucao: {_jobManager.RunningJobsCount}");
        }

        if (_trayService.HasActiveTunnel)
        {
            details.Add("- Tunel SSH ativo");
        }

        if (details.Count == 0)
        {
            return "Nenhuma operacao ativa detectada. Escolha uma opcao.";
        }

        return "Operacoes ativas detectadas:\n"
            + string.Join("\n", details)
            + "\n\nEscolha como deseja continuar.";
    }

    // --- Settings Navigation Logic ---

    private void ShowSettingsList()
    {
        SettingsListPanel.Visibility = Visibility.Visible;
        

        HarvestSettingsPanel.Visibility = Visibility.Collapsed;
        OrganizerSettingsPanel.Visibility = Visibility.Collapsed;
        MigrationsSettingsPanel.Visibility = Visibility.Collapsed;
        NgrokSettingsPanel.Visibility = Visibility.Collapsed;
        NotesCloudSettingsPanel.Visibility = Visibility.Collapsed;
        ToolProfilesSettingsPanel.Visibility = Visibility.Collapsed;
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

        _selectedToolProfile.Name = ToolProfileNameInput.Text;
        _selectedToolProfile.IsDefault = ToolProfileIsDefaultCheck.IsChecked == true;

        // Coletar valores dos campos dinâmicos recursivamente
        CollectProfileOptions(ToolProfileFieldsContainer, _selectedToolProfile.Options);

        _profileUIService.SaveProfile(_currentToolForProfiles, _selectedToolProfile);
        ToolProfilesList.Items.Refresh();
        UiMessageService.ShowInfo("Perfil salvo com sucesso!", "Sucesso");
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

            if (int.TryParse(HarvestMaxFileSizeInput.Text, out int size))
                _currentHarvestConfig.Rules.MaxFileSizeKb = size;
            else
                _currentHarvestConfig.Rules.MaxFileSizeKb = null;

            // Parse Limits
            int.TryParse(HarvestMinScore.Text, out int minScore); _currentHarvestConfig.MinScoreDefault = minScore;
            int.TryParse(HarvestTopDefault.Text, out int topDefault); _currentHarvestConfig.TopDefault = topDefault;

            // Parse Weights
            double.TryParse(HarvestWeightFanIn.Text, out double fanIn); _currentHarvestConfig.Weights.FanInWeight = fanIn;
            double.TryParse(HarvestWeightFanOut.Text, out double fanOut); _currentHarvestConfig.Weights.FanOutWeight = fanOut;
            double.TryParse(HarvestWeightDensity.Text, out double density); _currentHarvestConfig.Weights.KeywordDensityWeight = density;
            double.TryParse(HarvestWeightDeadCode.Text, out double deadCode); _currentHarvestConfig.Weights.DeadCodePenalty = deadCode;

            _configService.SaveSection("Harvest", _currentHarvestConfig);
            UiMessageService.ShowInfo("Configuração do Harvest salva com sucesso!", "Sucesso");
        }
        catch (Exception ex)
        {
            UiMessageService.ShowError("Erro ao salvar configuração do Harvest.", "Erro ao salvar", ex);
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

        _selectedCategory.Name = OrgCatName.Text;
        _selectedCategory.Folder = OrgCatFolder.Text;
        _selectedCategory.Keywords = OrgCatKeywords.Text
            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToArray();

        _configService.SaveSection("Organizer", _currentOrganizerConfig);
        OrganizerCategoriesList.Items.Refresh();
        UiMessageService.ShowInfo("Categoria salva!", "Sucesso");
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
        _currentMigrationsConfig.RootPath = MigRootPathSelector.SelectedPath;
        _currentMigrationsConfig.StartupProjectPath = MigStartupPathSelector.SelectedPath;
        _currentMigrationsConfig.DbContextFullName = MigContextInput.Text;
        _currentMigrationsConfig.AdditionalArgs = MigArgsInput.Text;

        _configService.SaveSection("Migrations", _currentMigrationsConfig);
        UiMessageService.ShowInfo("Configurações do Migrations salvas!", "Sucesso");
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
        _currentNgrokConfig = _configService.GetSection<NgrokSettings>("Ngrok");
        _currentNgrokConfig.Normalize();

        NgrokExeSelector.SelectedPath = _currentNgrokConfig.ExecutablePath;
        NgrokAuthTokenInput.Text = _currentNgrokConfig.AuthToken;
        NgrokArgsInput.Text = _currentNgrokConfig.AdditionalArgs;
    }

    private void SaveNgrokSettings_Click(object sender, RoutedEventArgs e)
    {
        _currentNgrokConfig.ExecutablePath = NgrokExeSelector.SelectedPath;
        _currentNgrokConfig.AuthToken = NgrokAuthTokenInput.Text;
        _currentNgrokConfig.AdditionalArgs = NgrokArgsInput.Text;

        _configService.SaveSection("Ngrok", _currentNgrokConfig);
        UiMessageService.ShowInfo("Configurações do Ngrok salvas!", "Sucesso");
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
            _currentNotesSettings.StoragePath = NotesStoragePathSelector.SelectedPath;
            _currentNotesSettings.AutoCloudSync = NotesAutoCloudSyncCheck.IsChecked == true;
            _currentNotesSettings.DefaultFormat = (NotesFormatCombo.SelectedItem as ComboBoxItem)?.Content.ToString() ?? ".txt";

            // Google Drive
            _currentGoogleDriveSettings = ReadGoogleDriveSettingsFromUi();
            if (_currentGoogleDriveSettings.IsEnabled && !ValidateGoogleDriveSettings(_currentGoogleDriveSettings, out var validationMessage))
            {
                ShowRequiredFieldsWarning(validationMessage);
                return;
            }

            _configService.SaveSection("Notes", _currentNotesSettings);
            _configService.SaveSection("GoogleDrive", _currentGoogleDriveSettings);

            UiMessageService.ShowInfo("Configurações de Notas e Nuvem salvas com sucesso!", "Sucesso");
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
        SetGDriveTestUiState(isTesting: true, statusText: "Testando conexão com Google Drive...");
        try
        {
            // Mantemos o fluxo assíncrono sem bloquear a UI.
            await _googleDriveService.TestConnectionAsync(tempSettings);

            SetGDriveTestUiState(isTesting: false, statusText: "Conexão validada com sucesso.");
            UiMessageService.ShowInfo("Conexão com Google Drive estabelecida com sucesso!", "Sucesso");
        }
        catch (Exception ex)
        {
            SetGDriveTestUiState(isTesting: false, statusText: "Falha ao validar conexão.", isError: true);
            UiMessageService.ShowError("Falha ao conectar com Google Drive. Verifique as credenciais.", "Erro de Conexão", ex);
        }
        finally
        {
            _isTestingGoogleDriveConnection = false;
            SetGDriveTestUiState(isTesting: false, statusText: GDriveTestConnectionStatus?.Text, isError: false, preserveStatus: true);
        }
    }

    private static void ShowRequiredFieldsWarning(string message)
    {
        UiMessageService.ShowWarning(message, "Campos Obrigatórios");
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
            GDriveTestConnectionButton.Content = isTesting ? "Testando..." : "Testar Conexão";
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

