using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using DevTools.Presentation.Wpf.Services;
using DevTools.SSHTunnel.Engine;
using DevTools.SSHTunnel.Models;
using DevTools.SSHTunnel.Providers;
using Microsoft.Win32;

namespace DevTools.Presentation.Wpf.Views;

public partial class SshTunnelWindow : Window
{
    private readonly JobManager _jobManager;
    private readonly SettingsService _settingsService;
    private readonly ConfigService _configService;
    private readonly TunnelService _tunnelService;
    
    private SshConfigSection _sshConfig = new();
    private TunnelProfile? _selectedProfile;
    private bool _isDirty;

    public SshTunnelWindow(JobManager jobManager, SettingsService settingsService, ConfigService configService)
    {
        InitializeComponent();
        _jobManager = jobManager;
        _settingsService = settingsService;
        _configService = configService;
        
        // Inicializa serviços do SSH Tunnel
        // ConfigStore removido em favor do ConfigService
        _tunnelService = new TunnelService(new SystemProcessRunner());

        LoadConfig();
        
        // Monitora fechamento para salvar posição
        Closing += (s, e) => SavePosition();
        
        // Timer para atualizar status UI (polling simples)
        var timer = new System.Windows.Threading.DispatcherTimer();
        timer.Tick += (s, e) => UpdateStatusUI();
        timer.Interval = TimeSpan.FromSeconds(1);
        timer.Start();
    }

    private void LoadConfig()
    {
        _sshConfig = _configService.GetSection<SshConfigSection>("Ssh");
        RefreshProfileList();
        
        if (_sshConfig.Profiles.Count > 0)
        {
            ProfilesList.SelectedIndex = 0;
        }
    }

    private void RefreshProfileList()
    {
        ProfilesList.ItemsSource = null;
        ProfilesList.ItemsSource = _sshConfig.Profiles;
    }

    private void ProfilesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProfilesList.SelectedItem is TunnelProfile profile)
        {
            _selectedProfile = profile;
            PopulateForm(profile);
            _isDirty = false;
            UpdateStatusUI();
        }
    }

    private void PopulateForm(TunnelProfile p)
    {
        ProfileNameInput.Text = p.Name;
        SshHostInput.Text = p.SshHost;
        SshPortInput.Text = p.SshPort.ToString();
        SshUserInput.Text = p.SshUser;
        IdentityFileInput.Text = p.IdentityFile ?? "";
        LocalBindInput.Text = p.LocalBindHost;
        LocalPortInput.Text = p.LocalPort.ToString();
        RemoteHostInput.Text = p.RemoteHost;
        RemotePortInput.Text = p.RemotePort.ToString();
    }

    private void UpdateModelFromForm()
    {
        if (_selectedProfile == null) return;

        _selectedProfile.Name = ProfileNameInput.Text;
        _selectedProfile.SshHost = SshHostInput.Text;
        int.TryParse(SshPortInput.Text, out int sshPort);
        _selectedProfile.SshPort = sshPort > 0 ? sshPort : 22;
        _selectedProfile.SshUser = SshUserInput.Text;
        _selectedProfile.IdentityFile = IdentityFileInput.Text;
        _selectedProfile.LocalBindHost = LocalBindInput.Text;
        int.TryParse(LocalPortInput.Text, out int localPort);
        _selectedProfile.LocalPort = localPort;
        _selectedProfile.RemoteHost = RemoteHostInput.Text;
        int.TryParse(RemotePortInput.Text, out int remotePort);
        _selectedProfile.RemotePort = remotePort;
    }

    private void Input_Changed(object sender, TextChangedEventArgs e)
    {
        _isDirty = true;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedProfile != null)
        {
            UpdateModelFromForm();
            _configService.SaveSection("Ssh", _sshConfig);
            _isDirty = false;
            RefreshProfileList();
            ProfilesList.SelectedItem = _selectedProfile; // Mantém seleção
            MessageBox.Show("Configurações salvas!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void AddProfile_Click(object sender, RoutedEventArgs e)
    {
        var newProfile = new TunnelProfile
        {
            Name = "Novo Perfil",
            SshHost = "hostname",
            SshUser = "user",
            IdentityFile = ""
        };
        _sshConfig.Profiles.Add(newProfile);
        _configService.SaveSection("Ssh", _sshConfig); // Save immediately
        RefreshProfileList();
        ProfilesList.SelectedItem = newProfile;
    }

    private void RemoveProfile_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedProfile != null)
        {
            if (MessageBox.Show($"Tem certeza que deseja remover o perfil '{_selectedProfile.Name}'?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                _sshConfig.Profiles.Remove(_selectedProfile);
                _configService.SaveSection("Ssh", _sshConfig);
                RefreshProfileList();
                if (_sshConfig.Profiles.Count > 0)
                    ProfilesList.SelectedIndex = 0;
                else
                    ClearForm();
            }
        }
    }

    private void ClearForm()
    {
        ProfileNameInput.Text = "";
        SshHostInput.Text = "";
        SshPortInput.Text = "";
        SshUserInput.Text = "";
        IdentityFileInput.Text = "";
        LocalBindInput.Text = "";
        LocalPortInput.Text = "";
        RemoteHostInput.Text = "";
        RemotePortInput.Text = "";
        _selectedProfile = null;
    }

    private async void ToggleTunnel_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedProfile == null) return;

        if (_tunnelService.IsOn)
        {
            // Stop
            ToggleTunnelButton.IsEnabled = false;
            ToggleTunnelButton.Content = "Parando...";
            await _tunnelService.StopAsync(TimeSpan.FromSeconds(5));
            UpdateStatusUI();
            ToggleTunnelButton.IsEnabled = true;
        }
        else
        {
            // Start
            // Primeiro salva se houver mudanças
            if (_isDirty)
            {
                UpdateModelFromForm();
                _configService.SaveSection("Ssh", _sshConfig);
                _isDirty = false;
            }

            ToggleTunnelButton.IsEnabled = false;
            ToggleTunnelButton.Content = "Conectando...";
            
            try
            {
                await _tunnelService.StartAsync(_selectedProfile);
            }
            catch (Exception ex)
            {
                AppLogger.Error("Error starting SSH tunnel", ex);
                MessageBox.Show($"Erro ao conectar:\n{ex.Message}\n\nDetalhes:\n{_tunnelService.LastError}", "Erro de Conexão", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                UpdateStatusUI();
                ToggleTunnelButton.IsEnabled = true;
            }
        }
    }

    private void UpdateStatusUI()
    {
        if (_tunnelService.IsOn)
        {
            StatusIndicator.Fill = new SolidColorBrush(Colors.LightGreen);
            StatusText.Text = $"Conectado (PID: {_tunnelService.ProcessId})";
            ToggleTunnelButton.Content = "Desconectar";
            ToggleTunnelButton.Background = new SolidColorBrush(Color.FromRgb(220, 53, 69)); // Red
        }
        else
        {
            StatusIndicator.Fill = new SolidColorBrush(Colors.Gray);
            StatusText.Text = "Desconectado";
            ToggleTunnelButton.Content = "Conectar";
            ToggleTunnelButton.Background = new SolidColorBrush(Color.FromRgb(40, 167, 69)); // Green
            
            if (_tunnelService.State == TunnelState.Error)
            {
                StatusIndicator.Fill = new SolidColorBrush(Colors.Red);
                StatusText.Text = "Erro (Verifique logs)";
            }
        }
    }

    private void BrowseKey_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Key Files (*.pem;*.ppk;*.key)|*.pem;*.ppk;*.key|All files (*.*)|*.*",
            Title = "Selecione a chave privada"
        };
        if (dlg.ShowDialog() == true)
        {
            IdentityFileInput.Text = dlg.FileName;
        }
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // DragMove();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void SavePosition()
    {
        _settingsService.Settings.SshWindowTop = Top;
        _settingsService.Settings.SshWindowLeft = Left;
        _settingsService.Save();
        
        // Stop tunnel on close
        _tunnelService.Dispose();
    }
}
