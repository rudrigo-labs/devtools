using System.Collections.Generic;
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
    private readonly ProfileManager _profileManager;
    private readonly TunnelService _tunnelService;
    private readonly bool _ownsTunnelService;
    private ToolProfile? _currentProfile;
    
    public SshTunnelWindow(
        JobManager jobManager,
        SettingsService settingsService,
        ConfigService configService,
        ProfileManager profileManager,
        TunnelService? sharedTunnelService = null)
    {
        InitializeComponent();
        _jobManager = jobManager;
        _settingsService = settingsService;
        _configService = configService;
        _profileManager = profileManager;
        
        // Inicializa serviços do SSH Tunnel
        _tunnelService = sharedTunnelService ?? new TunnelService(new SystemProcessRunner());
        _ownsTunnelService = sharedTunnelService == null;

        Loaded += OnLoaded;

        // Monitora fechamento para salvar posição
        Closing += (s, e) => SavePosition();
        Closed += (s, e) =>
        {
            if (_ownsTunnelService)
            {
                _tunnelService.Dispose();
            }
        };
        
        // Timer para atualizar status UI (polling simples)
        var timer = new System.Windows.Threading.DispatcherTimer();
        timer.Tick += (s, e) => UpdateStatusUI();
        timer.Interval = TimeSpan.FromSeconds(1);
        timer.Start();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Tentar carregar perfil padrão
        _currentProfile = _profileManager?.GetDefaultProfile("SSHTunnel");
        if (_currentProfile != null)
        {
            LoadProfile(_currentProfile);
        }
        else
        {
            // Fallback para configurações salvas anteriormente se houver (opcional)
        }
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

    private void UpdateProfileFromUi(ToolProfile profile)
    {
        profile.Options["ssh-host"] = SshHostInput.Text;
        profile.Options["ssh-port"] = SshPortInput.Text;
        profile.Options["ssh-user"] = SshUserInput.Text;
        profile.Options["identity-file"] = IdentityFileInput.Text;
        profile.Options["local-bind"] = LocalBindInput.Text;
        profile.Options["local-port"] = LocalPortInput.Text;
        profile.Options["remote-host"] = RemoteHostInput.Text;
        profile.Options["remote-port"] = RemotePortInput.Text;
    }

    private async void ToggleTunnel_Click(object sender, RoutedEventArgs e)
    {
        var primaryButton = MainFrame.PrimaryButton;
        if (primaryButton == null) return;

        if (_tunnelService.IsOn)
        {
            // Stop
            primaryButton.IsEnabled = false;
            primaryButton.Content = "Parando...";
            await _tunnelService.StopAsync(TimeSpan.FromSeconds(5));
            UpdateStatusUI();
            primaryButton.IsEnabled = true;
        }
        else
        {
            // Start
            if (!ValidateInputs(out var validationError))
            {
                UiMessageService.ShowError(validationError, "Erro de Validação");
                return;
            }

            var profile = BuildProfileFromUi();
            if (string.IsNullOrWhiteSpace(profile.SshHost))
            {
                UiMessageService.ShowError("Host SSH é obrigatório.", "Erro");
                return;
            }

            // Sincronizar com o perfil padrão se estiver em uso
            if (_currentProfile != null)
            {
                UpdateProfileFromUi(_currentProfile);
                _profileManager.SaveProfile("SSHTunnel", _currentProfile);
            }

            primaryButton.IsEnabled = false;
            primaryButton.Content = "Conectando...";
            
            try
            {
                await _tunnelService.StartAsync(profile);
            }
        catch (Exception ex)
        {
            UiMessageService.ShowError("Erro ao iniciar túnel SSH.", "Erro ao iniciar túnel", ex);
        }
            finally
            {
                primaryButton.IsEnabled = true;
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
            Name = "Manual",
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

    private bool ValidateInputs(out string errorMessage)
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(SshHostInput.Text))
            missing.Add("Host SSH");

        if (missing.Count > 0)
        {
            errorMessage = "Os campos abaixo não podem ficar em branco:\n- " + string.Join("\n- ", missing);
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    private void UpdateStatusUI()
    {
        var primaryButton = MainFrame.PrimaryButton;
        if (primaryButton == null) return;

        if (_tunnelService.IsOn)
        {
            MainFrame.StatusText = "🟢 Conectado";
            primaryButton.Content = "Desconectar";
            primaryButton.Style = (Style)FindResource("SecondaryButtonStyle"); // Use secondary style for disconnect
        }
        else
        {
            MainFrame.StatusText = "⚫ Parado";
            primaryButton.Content = "Conectar";
            primaryButton.Style = (Style)FindResource("PrimaryButtonStyle");
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

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
