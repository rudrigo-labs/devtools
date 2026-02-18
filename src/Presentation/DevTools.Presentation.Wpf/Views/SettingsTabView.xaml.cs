using System;
using System.Linq;
using System.Windows;
using WpfControls = System.Windows.Controls;
using DevTools.Harvest.Configuration;
using DevTools.Migrations.Models;
using DevTools.Ngrok.Models;
using DevTools.Organizer.Models;
using DevTools.Presentation.Wpf.Services;
using DevTools.SSHTunnel.Models;

namespace DevTools.Presentation.Wpf.Views;

public partial class SettingsTabView : WpfControls.UserControl
{
    public ConfigService? ConfigService { get; set; }

    private SshConfigSection _currentSshConfig = new();
    private HarvestConfig _currentHarvestConfig = new();
    private OrganizerConfig _currentOrganizerConfig = new();
    private MigrationsSettings _currentMigrationsConfig = new();
    private NgrokSettings _currentNgrokConfig = new();

    private TunnelProfile? _selectedProfile;
    private OrganizerCategory? _selectedCategory;

    public SettingsTabView()
    {
        InitializeComponent();
        ShowSettingsList();
    }

    public void ShowSettingsList()
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

    private void OpenSshSettings_Click(object sender, RoutedEventArgs e)
    {
        SettingsListPanel.Visibility = Visibility.Collapsed;
        SshSettingsPanel.Visibility = Visibility.Visible;
        LoadSshProfiles();
    }

    private void LoadSshProfiles()
    {
        if (ConfigService == null) return;

        _currentSshConfig = ConfigService.GetSection<SshConfigSection>("Ssh");
        SshProfilesList.ItemsSource = null;
        SshProfilesList.ItemsSource = _currentSshConfig.Profiles;

        SshEditForm.Visibility = Visibility.Collapsed;
        SshProfilesList.SelectedItem = null;
    }

    private void AddSshProfile_Click(object sender, RoutedEventArgs e)
    {
        if (ConfigService == null) return;

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

    private void SshProfile_SelectionChanged(object sender, WpfControls.SelectionChangedEventArgs e)
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
        if (ConfigService == null) return;
        if (_selectedProfile == null) return;

        _selectedProfile.Name = SshProfileName.Text;
        _selectedProfile.SshHost = SshHost.Text;
        int.TryParse(SshPort.Text, out int sshPort);
        _selectedProfile.SshPort = sshPort;
        _selectedProfile.SshUser = SshUser.Text;
        _selectedProfile.IdentityFile = SshKeyFile.Text;
        _selectedProfile.LocalBindHost = LocalBind.Text;
        int.TryParse(LocalPort.Text, out int localPort);
        _selectedProfile.LocalPort = localPort;
        _selectedProfile.RemoteHost = RemoteHost.Text;
        int.TryParse(RemotePort.Text, out int remotePort);
        _selectedProfile.RemotePort = remotePort;

        ConfigService.SaveSection("Ssh", _currentSshConfig);
        SshProfilesList.Items.Refresh();
        System.Windows.MessageBox.Show("Configuração salva com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void DeleteSshProfile_Click(object sender, RoutedEventArgs e)
    {
        if (ConfigService == null) return;
        if (_selectedProfile == null) return;

        if (System.Windows.MessageBox.Show($"Tem certeza que deseja excluir o perfil '{_selectedProfile.Name}'?", "Confirmar Exclusão", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
        {
            _currentSshConfig.Profiles.Remove(_selectedProfile);
            ConfigService.SaveSection("Ssh", _currentSshConfig);
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

    private void OpenHarvestSettings_Click(object sender, RoutedEventArgs e)
    {
        SettingsListPanel.Visibility = Visibility.Collapsed;
        HarvestSettingsPanel.Visibility = Visibility.Visible;
        LoadHarvestConfig();
    }

    private void LoadHarvestConfig()
    {
        if (ConfigService == null) return;

        _currentHarvestConfig = ConfigService.GetSection<HarvestConfig>("Harvest");
        _currentHarvestConfig.Normalize();

        HarvestExtensions.Text = string.Join(", ", _currentHarvestConfig.Rules.Extensions);
        HarvestExcludeDirs.Text = string.Join(", ", _currentHarvestConfig.Rules.ExcludeDirectories);
        HarvestMaxFileSizeInput.Text = _currentHarvestConfig.Rules.MaxFileSizeKb?.ToString() ?? "";

        HarvestMinScore.Text = _currentHarvestConfig.MinScoreDefault.ToString();
        HarvestTopDefault.Text = _currentHarvestConfig.TopDefault.ToString();

        HarvestWeightFanIn.Text = _currentHarvestConfig.Weights.FanInWeight.ToString("F1");
        HarvestWeightFanOut.Text = _currentHarvestConfig.Weights.FanOutWeight.ToString("F1");
        HarvestWeightDensity.Text = _currentHarvestConfig.Weights.KeywordDensityWeight.ToString("F1");
        HarvestWeightDeadCode.Text = _currentHarvestConfig.Weights.DeadCodePenalty.ToString("F1");
    }

    private void SaveHarvestSettings_Click(object sender, RoutedEventArgs e)
    {
        if (ConfigService == null) return;

        try
        {
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

            int.TryParse(HarvestMinScore.Text, out int minScore);
            _currentHarvestConfig.MinScoreDefault = minScore;
            int.TryParse(HarvestTopDefault.Text, out int topDefault);
            _currentHarvestConfig.TopDefault = topDefault;

            double.TryParse(HarvestWeightFanIn.Text, out double fanIn);
            _currentHarvestConfig.Weights.FanInWeight = fanIn;
            double.TryParse(HarvestWeightFanOut.Text, out double fanOut);
            _currentHarvestConfig.Weights.FanOutWeight = fanOut;
            double.TryParse(HarvestWeightDensity.Text, out double density);
            _currentHarvestConfig.Weights.KeywordDensityWeight = density;
            double.TryParse(HarvestWeightDeadCode.Text, out double deadCode);
            _currentHarvestConfig.Weights.DeadCodePenalty = deadCode;

            ConfigService.SaveSection("Harvest", _currentHarvestConfig);
            System.Windows.MessageBox.Show("Configuração do Harvest salva com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Erro ao salvar: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenOrganizerSettings_Click(object sender, RoutedEventArgs e)
    {
        SettingsListPanel.Visibility = Visibility.Collapsed;
        OrganizerSettingsPanel.Visibility = Visibility.Visible;
        LoadOrganizerConfig();
    }

    private void LoadOrganizerConfig()
    {
        if (ConfigService == null) return;

        _currentOrganizerConfig = ConfigService.GetSection<OrganizerConfig>("Organizer");
        if (_currentOrganizerConfig.Categories == null) _currentOrganizerConfig.Categories = new();

        OrganizerCategoriesList.ItemsSource = null;
        OrganizerCategoriesList.ItemsSource = _currentOrganizerConfig.Categories;
        OrganizerCategoriesList.SelectedItem = null;
        OrganizerEditForm.Visibility = Visibility.Collapsed;
    }

    private void AddOrganizerCategory_Click(object sender, RoutedEventArgs e)
    {
        if (ConfigService == null) return;

        var newCat = new OrganizerCategory("Nova Categoria", "NovaPasta", Array.Empty<string>());
        _currentOrganizerConfig.Categories.Add(newCat);
        OrganizerCategoriesList.ItemsSource = null;
        OrganizerCategoriesList.ItemsSource = _currentOrganizerConfig.Categories;
        OrganizerCategoriesList.SelectedItem = newCat;
    }

    private void OrganizerCategory_SelectionChanged(object sender, WpfControls.SelectionChangedEventArgs e)
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
        if (ConfigService == null) return;
        if (_selectedCategory == null) return;

        _selectedCategory.Name = OrgCatName.Text;
        _selectedCategory.Folder = OrgCatFolder.Text;
        _selectedCategory.Keywords = OrgCatKeywords.Text
            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToArray();

        ConfigService.SaveSection("Organizer", _currentOrganizerConfig);
        OrganizerCategoriesList.Items.Refresh();
        System.Windows.MessageBox.Show("Categoria salva!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void DeleteOrganizerCategory_Click(object sender, RoutedEventArgs e)
    {
        if (ConfigService == null) return;
        if (_selectedCategory == null) return;

        if (System.Windows.MessageBox.Show($"Excluir categoria '{_selectedCategory.Name}'?", "Confirmar", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
        {
            _currentOrganizerConfig.Categories.Remove(_selectedCategory);
            ConfigService.SaveSection("Organizer", _currentOrganizerConfig);
            LoadOrganizerConfig();
        }
    }

    private void OpenMigrationsSettings_Click(object sender, RoutedEventArgs e)
    {
        SettingsListPanel.Visibility = Visibility.Collapsed;
        MigrationsSettingsPanel.Visibility = Visibility.Visible;
        LoadMigrationsConfig();
    }

    private void LoadMigrationsConfig()
    {
        if (ConfigService == null) return;

        _currentMigrationsConfig = ConfigService.GetSection<MigrationsSettings>("Migrations");

        MigRootPathInput.Text = _currentMigrationsConfig.RootPath;
        MigStartupPathInput.Text = _currentMigrationsConfig.StartupProjectPath;
        MigContextInput.Text = _currentMigrationsConfig.DbContextFullName;
        MigArgsInput.Text = _currentMigrationsConfig.AdditionalArgs;
    }

    private void SaveMigrationsSettings_Click(object sender, RoutedEventArgs e)
    {
        if (ConfigService == null) return;

        _currentMigrationsConfig.RootPath = MigRootPathInput.Text;
        _currentMigrationsConfig.StartupProjectPath = MigStartupPathInput.Text;
        _currentMigrationsConfig.DbContextFullName = MigContextInput.Text;
        _currentMigrationsConfig.AdditionalArgs = MigArgsInput.Text;

        ConfigService.SaveSection("Migrations", _currentMigrationsConfig);
        System.Windows.MessageBox.Show("Configurações do Migrations salvas!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OpenNgrokSettings_Click(object sender, RoutedEventArgs e)
    {
        SettingsListPanel.Visibility = Visibility.Collapsed;
        NgrokSettingsPanel.Visibility = Visibility.Visible;
        LoadNgrokConfig();
    }

    private void LoadNgrokConfig()
    {
        if (ConfigService == null) return;

        _currentNgrokConfig = ConfigService.GetSection<NgrokSettings>("Ngrok");
        _currentNgrokConfig.Normalize();

        NgrokExePathInput.Text = _currentNgrokConfig.ExecutablePath;
        NgrokAuthTokenInput.Text = _currentNgrokConfig.AuthToken;
        NgrokArgsInput.Text = _currentNgrokConfig.AdditionalArgs;
    }

    private void SaveNgrokSettings_Click(object sender, RoutedEventArgs e)
    {
        if (ConfigService == null) return;

        _currentNgrokConfig.ExecutablePath = NgrokExePathInput.Text;
        _currentNgrokConfig.AuthToken = NgrokAuthTokenInput.Text;
        _currentNgrokConfig.AdditionalArgs = NgrokArgsInput.Text;

        ConfigService.SaveSection("Ngrok", _currentNgrokConfig);
        System.Windows.MessageBox.Show("Configurações do Ngrok salvas!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
