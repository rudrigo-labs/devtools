using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
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

        _tunnelService = sharedTunnelService ?? new TunnelService(new SystemProcessRunner());
        _ownsTunnelService = sharedTunnelService == null;

        Loaded += OnLoaded;

        Closing += (s, e) => SavePosition();
        Closed += (s, e) =>
        {
            if (_ownsTunnelService)
            {
                _tunnelService.Dispose();
            }
        };

        var timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        timer.Tick += (s, e) => UpdateStatusUI();
        timer.Start();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _currentProfile = _profileManager?.GetDefaultProfile("SSHTunnel");
        if (_currentProfile != null)
        {
            LoadProfile(_currentProfile);
        }
    }

    private void LoadProfile(ToolProfile profile)
    {
        if (profile.Options.TryGetValue("ssh-host", out var sshHost)) SshHostInput.Text = sshHost;
        if (profile.Options.TryGetValue("ssh-port", out var sshPort)) SshPortInput.Text = sshPort;
        if (profile.Options.TryGetValue("ssh-user", out var sshUser)) SshUserInput.Text = sshUser;
        if (profile.Options.TryGetValue("identity-file", out var identityFile)) IdentityFileSelector.SelectedPath = identityFile;
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
        profile.Options["identity-file"] = IdentityFileSelector.SelectedPath ?? string.Empty;
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
            primaryButton.IsEnabled = false;
            primaryButton.Content = "Parando...";
            await _tunnelService.StopAsync(TimeSpan.FromSeconds(5));
            UpdateStatusUI();
            primaryButton.IsEnabled = true;
            return;
        }

        if (!ValidateInputs(out var validationError))
        {
            ValidationUiService.ShowInline(MainFrame, validationError);
            return;
        }

        ValidationUiService.ClearInline(MainFrame);

        var profile = BuildProfileFromUi();

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
            UiMessageService.ShowError("Erro ao iniciar tunel SSH.", "Erro ao iniciar tunel", ex);
        }
        finally
        {
            primaryButton.IsEnabled = true;
            UpdateStatusUI();
        }
    }

    private TunnelProfile BuildProfileFromUi()
    {
        _ = int.TryParse(SshPortInput.Text, out var sshPort);
        _ = int.TryParse(LocalPortInput.Text, out var localPort);
        _ = int.TryParse(RemotePortInput.Text, out var remotePort);

        return new TunnelProfile
        {
            Name = "Manual",
            SshHost = SshHostInput.Text,
            SshPort = sshPort > 0 ? sshPort : 22,
            SshUser = SshUserInput.Text,
            IdentityFile = IdentityFileSelector.SelectedPath ?? string.Empty,
            LocalBindHost = LocalBindInput.Text,
            LocalPort = localPort,
            RemoteHost = RemoteHostInput.Text,
            RemotePort = remotePort
        };
    }

    private bool ValidateInputs(out string errorMessage)
    {
        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(SshHostInput.Text)) missing.Add("Host SSH");
        if (string.IsNullOrWhiteSpace(SshPortInput.Text)) missing.Add("Porta SSH");
        if (string.IsNullOrWhiteSpace(SshUserInput.Text)) missing.Add("Usuario SSH");
        if (string.IsNullOrWhiteSpace(IdentityFileSelector.SelectedPath)) missing.Add("Arquivo de Chave");
        if (string.IsNullOrWhiteSpace(LocalBindInput.Text)) missing.Add("Bind Local");
        if (string.IsNullOrWhiteSpace(LocalPortInput.Text)) missing.Add("Porta Local");
        if (string.IsNullOrWhiteSpace(RemoteHostInput.Text)) missing.Add("Host Remoto");
        if (string.IsNullOrWhiteSpace(RemotePortInput.Text)) missing.Add("Porta Remota");

        if (missing.Count > 0)
        {
            errorMessage = "Os campos abaixo nao podem ficar em branco:\n- " + string.Join("\n- ", missing);
            return false;
        }

        if (!int.TryParse(SshPortInput.Text, out _)
            || !int.TryParse(LocalPortInput.Text, out _)
            || !int.TryParse(RemotePortInput.Text, out _))
        {
            errorMessage = "Portas SSH, Local e Remota devem ser numericas.";
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
            MainFrame.StatusText = "Status: Conectado";
            primaryButton.Content = "Desconectar";
            primaryButton.Style = (Style)FindResource("SecondaryButtonStyle");
            return;
        }

        MainFrame.StatusText = "Status: Parado";
        primaryButton.Content = "Conectar";
        primaryButton.Style = (Style)FindResource("PrimaryButtonStyle");
    }

    private void SavePosition()
    {
        // Sem persistencia de posicao no momento.
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
