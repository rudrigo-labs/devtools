using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DevTools.Core.Configuration;
using DevTools.Core.Models;
using DevTools.Presentation.Wpf.Services;
using DevTools.SSHTunnel.Engine;
using DevTools.SSHTunnel.Models;
using DevTools.SSHTunnel.Providers;

namespace DevTools.Presentation.Wpf.Views;

public partial class SshTunnelWindow : Window
{
    private readonly JobManager _jobManager;
    private readonly SettingsService _settingsService;
    private readonly ConfigService _configService;
    private readonly TunnelService _tunnelService;
    
    public SshTunnelWindow(JobManager jobManager, SettingsService settingsService, ConfigService configService)
    {
        InitializeComponent();
        _jobManager = jobManager;
        _settingsService = settingsService;
        _configService = configService;
        
        // Inicializa serviços do SSH Tunnel
        _tunnelService = new TunnelService(new SystemProcessRunner());

        // Configura ProfileSelector
        ProfileSelector.ProfileLoaded += LoadProfile;
        ProfileSelector.GetOptionsFunc = GetCurrentOptions;
        
        // Monitora fechamento para salvar posição
        Closing += (s, e) => SavePosition();
        
        // Timer para atualizar status UI (polling simples)
        var timer = new System.Windows.Threading.DispatcherTimer();
        timer.Tick += (s, e) => UpdateStatusUI();
        timer.Interval = TimeSpan.FromSeconds(1);
        timer.Start();
    }

    private void LoadProfile(ToolProfile profile)
    {
        if (profile.Options.TryGetValue("ssh-host", out var sshHost)) SshHostInput.Text = sshHost;
        if (profile.Options.TryGetValue("ssh-port", out var sshPort)) SshPortInput.Text = sshPort;
        if (profile.Options.TryGetValue("ssh-user", out var sshUser)) SshUserInput.Text = sshUser;
        if (profile.Options.TryGetValue("identity-file", out var identityFile)) IdentityFileInput.Text = identityFile;
        if (profile.Options.TryGetValue("local-bind", out var localBind)) LocalBindInput.Text = localBind;
        if (profile.Options.TryGetValue("local-port", out var localPort)) LocalPortInput.Text = localPort;
        if (profile.Options.TryGetValue("remote-host", out var remoteHost)) RemoteHostInput.Text = remoteHost;
        if (profile.Options.TryGetValue("remote-port", out var remotePort)) RemotePortInput.Text = remotePort;
    }

    private Dictionary<string, string> GetCurrentOptions()
    {
        var options = new Dictionary<string, string>();
        options["ssh-host"] = SshHostInput.Text;
        options["ssh-port"] = SshPortInput.Text;
        options["ssh-user"] = SshUserInput.Text;
        options["identity-file"] = IdentityFileInput.Text;
        options["local-bind"] = LocalBindInput.Text;
        options["local-port"] = LocalPortInput.Text;
        options["remote-host"] = RemoteHostInput.Text;
        options["remote-port"] = RemotePortInput.Text;
        return options;
    }

    private async void ToggleTunnel_Click(object sender, RoutedEventArgs e)
    {
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
            var profile = BuildProfileFromUi();
            if (string.IsNullOrWhiteSpace(profile.SshHost))
            {
                MessageBox.Show("Host SSH é obrigatório.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ToggleTunnelButton.IsEnabled = false;
            ToggleTunnelButton.Content = "Conectando...";
            
            try
            {
                await _tunnelService.StartAsync(profile);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao iniciar túnel: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ToggleTunnelButton.IsEnabled = true;
                UpdateStatusUI();
            }
        }
    }

    private TunnelProfile BuildProfileFromUi()
    {
        int.TryParse(SshPortInput.Text, out int sshPort);
        int.TryParse(LocalPortInput.Text, out int localPort);
        int.TryParse(RemotePortInput.Text, out int remotePort);

        return new TunnelProfile
        {
            Name = ProfileSelector.SelectedProfile?.Name ?? "Manual",
            SshHost = SshHostInput.Text,
            SshPort = sshPort > 0 ? sshPort : 22,
            SshUser = SshUserInput.Text,
            IdentityFile = IdentityFileInput.Text,
            LocalBindHost = LocalBindInput.Text,
            LocalPort = localPort,
            RemoteHost = RemoteHostInput.Text,
            RemotePort = remotePort
        };
    }

    private void UpdateStatusUI()
    {
        if (_tunnelService.IsOn)
        {
            StatusIndicator.Fill = Brushes.Green;
            StatusText.Text = "Conectado";
            ToggleTunnelButton.Content = "Desconectar";
            ToggleTunnelButton.Style = (Style)FindResource("SecondaryButtonStyle"); // Use secondary style for disconnect
        }
        else
        {
            StatusIndicator.Fill = Brushes.Gray;
            StatusText.Text = "Parado";
            ToggleTunnelButton.Content = "Conectar";
            ToggleTunnelButton.Style = (Style)FindResource("PrimaryButtonStyle");
        }
    }

    private void BrowseKey_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Key Files|*.pem;*.key;*.ppk|All Files|*.*",
            Title = "Selecione o arquivo de chave privada"
        };
        if (dialog.ShowDialog() == true)
        {
            IdentityFileInput.Text = dialog.FileName;
        }
    }

    private void SavePosition()
    {
        // Implementar persistência de posição da janela se necessário
    }

    private void Header_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
