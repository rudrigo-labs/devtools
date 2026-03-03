using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DevTools.Presentation.Wpf.Services;
using DevTools.SSHTunnel.Models;
using DevTools.Harvest.Configuration;
using DevTools.Organizer.Models;
using DevTools.Migrations.Models;
using DevTools.Ngrok.Models;
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

    // Config Objects
    private HarvestConfig _currentHarvestConfig = new();
    private OrganizerConfig _currentOrganizerConfig = new();
    private MigrationsSettings _currentMigrationsConfig = new();
    private NgrokSettings _currentNgrokConfig = new();

    // State
    private OrganizerCategory? _selectedCategory;
    private string? _currentToolForProfiles;
    private ToolProfile? _selectedToolProfile;
    private readonly ProfileUIService _profileUIService;

    public MainWindow(TrayService trayService, JobManager jobManager, ConfigService configService, ProfileUIService profileUIService)
    {
        InitializeComponent();
        _trayService = trayService;
        _jobManager = jobManager;
        _configService = configService;
        _profileUIService = profileUIService;

        _trayService.SetMainWindow(this);

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

    /// <summary>
    /// Botão Encerrar escolha:Sim
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Shutdown_Click(object sender, RoutedEventArgs e)
    {
        System.Windows.Application.Current.Shutdown();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
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
        // Se o usuário clicar no X ou Alt+F4, mostramos o diálogo em vez de esconder
        e.Cancel = true;
        CloseButton_Click(this, new RoutedEventArgs());
    }

    // --- Settings Navigation Logic ---

    private void ShowSettingsList()
    {
        SettingsListPanel.Visibility = Visibility.Visible;
        

        HarvestSettingsPanel.Visibility = Visibility.Collapsed;
        OrganizerSettingsPanel.Visibility = Visibility.Collapsed;
        MigrationsSettingsPanel.Visibility = Visibility.Collapsed;
        NgrokSettingsPanel.Visibility = Visibility.Collapsed;
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
}
