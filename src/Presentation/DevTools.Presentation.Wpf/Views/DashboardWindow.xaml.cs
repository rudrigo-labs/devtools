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

namespace DevTools.Presentation.Wpf.Views;

public partial class DashboardWindow : Window
{
    private readonly TrayService _trayService;
    private readonly JobManager _jobManager;
    private readonly ConfigService _configService;

    // Config Objects
    private SshConfigSection _currentSshConfig = new();
    private HarvestConfig _currentHarvestConfig = new();
    private OrganizerConfig _currentOrganizerConfig = new();
    private MigrationsSettings _currentMigrationsConfig = new();
    private NgrokSettings _currentNgrokConfig = new();

    // State
    private TunnelProfile? _selectedProfile;
    private OrganizerCategory? _selectedCategory;

    public DashboardWindow(TrayService trayService, JobManager jobManager, ConfigService configService)
    {
        InitializeComponent();
        _trayService = trayService;
        _jobManager = jobManager;
        _configService = configService;

        // Binding direto da coleção de Jobs
        JobsDataGrid.ItemsSource = _jobManager.Jobs;

        this.Loaded += DashboardWindow_Loaded;
        this.Closing += DashboardWindow_Closing;
    }

    private void DashboardWindow_Loaded(object sender, RoutedEventArgs e)
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

    private void Shutdown_Click(object sender, RoutedEventArgs e)
    {
        System.Windows.Application.Current.Shutdown();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Hide(); // Apenas esconde, não fecha a aplicação
    }

    private void DashboardWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // Impede que o fechamento encerre a aplicação; apenas oculta na bandeja
        e.Cancel = true;
        Hide();
    }

    // --- Settings Navigation Logic ---

    private void ShowSettingsList()
    {
        SettingsListPanel.Visibility = Visibility.Visible;
        
        SshSettingsPanel.Visibility = Visibility.Collapsed;
        HarvestSettingsPanel.Visibility = Visibility.Collapsed;
        OrganizerSettingsPanel.Visibility = Visibility.Collapsed;
        MigrationsSettingsPanel.Visibility = Visibility.Collapsed;
        NgrokSettingsPanel.Visibility = Visibility.Collapsed;
    }

    private void BackToSettingsList_Click(object sender, RoutedEventArgs e)
    {
        ShowSettingsList();
    }

    // --- SSH Settings ---

    private void OpenSshSettings_Click(object sender, RoutedEventArgs e)
    {
        SettingsListPanel.Visibility = Visibility.Collapsed;
        SshSettingsPanel.Visibility = Visibility.Visible;
        LoadSshProfiles();
    }

    private void LoadSshProfiles()
    {
        _currentSshConfig = _configService.GetSection<SshConfigSection>("Ssh");
        SshProfilesList.ItemsSource = null;
        SshProfilesList.ItemsSource = _currentSshConfig.Profiles;
        
        SshEditForm.Visibility = Visibility.Collapsed;
        SshProfilesList.SelectedItem = null;
    }

    private void AddSshProfile_Click(object sender, RoutedEventArgs e)
    {
        var newProfile = new TunnelProfile 
        { 
            Name = "Novo Perfil " + (_currentSshConfig.Profiles.Count + 1),
            SshPort = 22,
            LocalPort = 1433,
            RemotePort = 1433
        };
        _currentSshConfig.Profiles.Add(newProfile);
        
        SshProfilesList.ItemsSource = null;
        SshProfilesList.ItemsSource = _currentSshConfig.Profiles;
        SshProfilesList.SelectedItem = newProfile;
    }

    private void SshProfile_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SshProfilesList.SelectedItem is TunnelProfile profile)
        {
            _selectedProfile = profile;
            
            SshProfileName.Text = profile.Name;
            SshHost.Text = profile.SshHost;
            SshPort.Text = profile.SshPort.ToString();
            SshUser.Text = profile.SshUser;
            SshKeyFile.Text = profile.IdentityFile;
            LocalBind.Text = profile.LocalBindHost;
            LocalPort.Text = profile.LocalPort.ToString();
            RemoteHost.Text = profile.RemoteHost;
            RemotePort.Text = profile.RemotePort.ToString();

            SshEditForm.Visibility = Visibility.Visible;
        }
        else
        {
            _selectedProfile = null;
            SshEditForm.Visibility = Visibility.Collapsed;
        }
    }

    private void SaveSshProfile_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedProfile == null) return;

        _selectedProfile.Name = SshProfileName.Text;
        _selectedProfile.SshHost = SshHost.Text;
        int.TryParse(SshPort.Text, out int sshPort); _selectedProfile.SshPort = sshPort;
        _selectedProfile.SshUser = SshUser.Text;
        _selectedProfile.IdentityFile = SshKeyFile.Text;
        _selectedProfile.LocalBindHost = LocalBind.Text;
        int.TryParse(LocalPort.Text, out int localPort); _selectedProfile.LocalPort = localPort;
        _selectedProfile.RemoteHost = RemoteHost.Text;
        int.TryParse(RemotePort.Text, out int remotePort); _selectedProfile.RemotePort = remotePort;

        _configService.SaveSection("Ssh", _currentSshConfig);
        SshProfilesList.Items.Refresh();
        System.Windows.MessageBox.Show("Configuração salva com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void DeleteSshProfile_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedProfile == null) return;

        if (System.Windows.MessageBox.Show($"Tem certeza que deseja excluir o perfil '{_selectedProfile.Name}'?", "Confirmar Exclusão", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
        {
            _currentSshConfig.Profiles.Remove(_selectedProfile);
            _configService.SaveSection("Ssh", _currentSshConfig);
            LoadSshProfiles();
        }
    }

    private void BrowseSshKey_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            DefaultExt = ".pem",
            Filter = "Key Files (*.pem;*.ppk)|*.pem;*.ppk|All Files (*.*)|*.*"
        };

        if (dlg.ShowDialog() == true)
        {
            SshKeyFile.Text = dlg.FileName;
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
        System.Windows.MessageBox.Show("Configuração do Harvest salva com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Erro ao salvar: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
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
        System.Windows.MessageBox.Show("Categoria salva!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void DeleteOrganizerCategory_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedCategory == null) return;
        
        if (System.Windows.MessageBox.Show($"Excluir categoria '{_selectedCategory.Name}'?", "Confirmar", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
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
        
        MigRootPathInput.Text = _currentMigrationsConfig.RootPath;
        MigStartupPathInput.Text = _currentMigrationsConfig.StartupProjectPath;
        MigContextInput.Text = _currentMigrationsConfig.DbContextFullName;
        MigArgsInput.Text = _currentMigrationsConfig.AdditionalArgs;
    }

    private void SaveMigrationsSettings_Click(object sender, RoutedEventArgs e)
    {
        _currentMigrationsConfig.RootPath = MigRootPathInput.Text;
        _currentMigrationsConfig.StartupProjectPath = MigStartupPathInput.Text;
        _currentMigrationsConfig.DbContextFullName = MigContextInput.Text;
        _currentMigrationsConfig.AdditionalArgs = MigArgsInput.Text;

        _configService.SaveSection("Migrations", _currentMigrationsConfig);
        System.Windows.MessageBox.Show("Configurações do Migrations salvas!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
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

        NgrokExePathInput.Text = _currentNgrokConfig.ExecutablePath;
        NgrokAuthTokenInput.Text = _currentNgrokConfig.AuthToken;
        NgrokArgsInput.Text = _currentNgrokConfig.AdditionalArgs;
    }

    private void SaveNgrokSettings_Click(object sender, RoutedEventArgs e)
    {
        _currentNgrokConfig.ExecutablePath = NgrokExePathInput.Text;
        _currentNgrokConfig.AuthToken = NgrokAuthTokenInput.Text;
        _currentNgrokConfig.AdditionalArgs = NgrokArgsInput.Text;

        _configService.SaveSection("Ngrok", _currentNgrokConfig);
        System.Windows.MessageBox.Show("Configurações do Ngrok salvas!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
