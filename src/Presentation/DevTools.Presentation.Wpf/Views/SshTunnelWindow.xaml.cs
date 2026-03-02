using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using DevTools.Core.Models;
using DevTools.Presentation.Wpf.Services;
using DevTools.Presentation.Wpf.Utilities;
using DevTools.SSHTunnel.Engine;
using DevTools.SSHTunnel.Models;
using DevTools.SSHTunnel.Providers;

namespace DevTools.Presentation.Wpf.Views;

public partial class SshTunnelWindow : Window
{
    private readonly SettingsService _settingsService;
    private readonly TunnelService _tunnelService;
    private readonly DispatcherTimer _timer;

    public SshTunnelWindow(SettingsService settingsService)
    {
        InitializeComponent();
        _settingsService = settingsService;

        // Load Settings
        if (!string.IsNullOrEmpty(_settingsService.Settings.LastSshHost))
            SshHostBox.Text = _settingsService.Settings.LastSshHost;
        if (_settingsService.Settings.LastSshPort.HasValue)
            SshPortBox.Text = _settingsService.Settings.LastSshPort.Value.ToString();
        if (!string.IsNullOrEmpty(_settingsService.Settings.LastSshUser))
            SshUserBox.Text = _settingsService.Settings.LastSshUser;
        if (!string.IsNullOrEmpty(_settingsService.Settings.LastSshKeyPath))
            IdentityFileSelector.SelectedPath = _settingsService.Settings.LastSshKeyPath;
        if (!string.IsNullOrEmpty(_settingsService.Settings.LastSshRemoteHost))
            RemoteHostBox.Text = _settingsService.Settings.LastSshRemoteHost;
        if (_settingsService.Settings.LastSshRemotePort.HasValue)
            RemotePortBox.Text = _settingsService.Settings.LastSshRemotePort.Value.ToString();
        if (!string.IsNullOrEmpty(_settingsService.Settings.LastSshLocalBind))
            LocalBindBox.Text = _settingsService.Settings.LastSshLocalBind;
        if (_settingsService.Settings.LastSshLocalPort.HasValue)
            LocalPortBox.Text = _settingsService.Settings.LastSshLocalPort.Value.ToString();
        if (_settingsService.Settings.LastSshCompression.HasValue)
            CompressionCheck.IsChecked = _settingsService.Settings.LastSshCompression.Value;
        if (_settingsService.Settings.LastSshVerbose.HasValue)
            VerboseCheck.IsChecked = _settingsService.Settings.LastSshVerbose.Value;
        
        // Initialize TunnelService manually as in the original code
        _tunnelService = new TunnelService(new SystemProcessRunner());

        // Timer for status updates
        _timer = new DispatcherTimer();
        _timer.Tick += (s, e) => UpdateStatusUI();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Start();

        UpdateStatusUI();
    }

    private void UpdateStatusUI()
    {
        if (_tunnelService.IsOn)
        {
            StatusText.Text = "Conectado (Rodando)";
            StatusText.Foreground = System.Windows.Media.Brushes.LightGreen;
            ToggleTunnelButton.Content = "Desconectar";
            ToggleTunnelButton.Style = (Style)FindResource("DevToolsDangerButton"); // Assuming this style exists or fallback
            
            // Disable inputs while running
            SetInputsEnabled(false);
        }
        else
        {
            StatusText.Text = "Desconectado";
            StatusText.Foreground = System.Windows.Media.Brushes.Gray;
            ToggleTunnelButton.Content = "Conectar Túnel";
            ToggleTunnelButton.Style = (Style)FindResource("DevToolsPrimaryButton");
            
            SetInputsEnabled(true);
        }

        // Update logs if available
        if (_tunnelService.IsOn)
        {
             // If TunnelService exposed logs, we would update them here.
             // For now, we can show process ID or basic info
             LogBox.Text = $"Process ID: {_tunnelService.ProcessId}";
        }
        else
        {
             LogBox.Text = "Serviço parado.";
        }
    }

    private void SetInputsEnabled(bool enabled)
    {
        SshHostBox.IsEnabled = enabled;
        SshPortBox.IsEnabled = enabled;
        SshUserBox.IsEnabled = enabled;
        IdentityFileSelector.IsEnabled = enabled;
        RemoteHostBox.IsEnabled = enabled;
        RemotePortBox.IsEnabled = enabled;
        LocalBindBox.IsEnabled = enabled;
        LocalPortBox.IsEnabled = enabled;
        CompressionCheck.IsEnabled = enabled;
        VerboseCheck.IsEnabled = enabled;
    }

    private async void ToggleTunnel_Click(object sender, RoutedEventArgs e)
    {
        if (_tunnelService.IsOn)
        {
            ToggleTunnelButton.Content = "Parando...";
            ToggleTunnelButton.IsEnabled = false;
            
            try 
            {
                await _tunnelService.StopAsync(TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                AppLogger.Error("Erro ao parar túnel SSH", ex);
                DevToolsMessage.Error($"Erro ao parar: {ex.Message}", "Erro");
            }
            finally
            {
                ToggleTunnelButton.IsEnabled = true;
                UpdateStatusUI();
            }
        }
        else
        {
            // Validate
            if (string.IsNullOrWhiteSpace(SshHostBox.Text))
            {
                DevToolsMessage.Warning("Host SSH é obrigatório.", "Erro");
                return;
            }

            if (!string.IsNullOrEmpty(IdentityFileSelector.SelectedPath) && !System.IO.File.Exists(IdentityFileSelector.SelectedPath))
            {
                DevToolsMessage.Error("O arquivo de chave privada especificado não existe.", "Arquivo Inválido");
                return;
            }

            if (!int.TryParse(SshPortBox.Text, out var sshPort)) 
            {
                DevToolsMessage.Error("Porta SSH inválida.", "Erro");
                return;
            }

            if (!int.TryParse(RemotePortBox.Text, out var remotePort))
            {
                DevToolsMessage.Error("Porta Remota inválida.", "Erro");
                return;
            }

            if (!int.TryParse(LocalPortBox.Text, out var localPort))
            {
                DevToolsMessage.Error("Porta Local inválida.", "Erro");
                return;
            }

            // Save Settings
            _settingsService.Settings.LastSshHost = SshHostBox.Text;
            _settingsService.Settings.LastSshPort = sshPort;
            _settingsService.Settings.LastSshUser = SshUserBox.Text;
            _settingsService.Settings.LastSshKeyPath = IdentityFileSelector.SelectedPath;
            _settingsService.Settings.LastSshRemoteHost = RemoteHostBox.Text;
            _settingsService.Settings.LastSshRemotePort = remotePort;
            _settingsService.Settings.LastSshLocalBind = LocalBindBox.Text;
            _settingsService.Settings.LastSshLocalPort = localPort;
            _settingsService.Settings.LastSshCompression = CompressionCheck.IsChecked;
            _settingsService.Settings.LastSshVerbose = VerboseCheck.IsChecked;
            _settingsService.Save();

            var profile = new TunnelProfile
            {
                SshHost = SshHostBox.Text,
                SshPort = sshPort,
                SshUser = SshUserBox.Text,
                IdentityFile = IdentityFileSelector.SelectedPath,
                RemoteHost = RemoteHostBox.Text,
                RemotePort = remotePort,
                LocalBindHost = LocalBindBox.Text,
                LocalPort = localPort
                // Compression and Verbose are not supported by TunnelProfile yet
            };

            ToggleTunnelButton.Content = "Conectando...";
            ToggleTunnelButton.IsEnabled = false;

            try
            {
                await _tunnelService.StartAsync(profile);
            }
            catch (Exception ex)
            {
                AppLogger.Error("Erro ao iniciar túnel SSH", ex);
                DevToolsMessage.Error($"Erro ao iniciar túnel: {ex.Message}", "Erro");
            }
            finally
            {
                ToggleTunnelButton.IsEnabled = true;
                UpdateStatusUI();
            }
        }
    }

    private void Header_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        DragMove();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        if (_tunnelService.IsOn)
        {
             if (DevToolsMessage.Confirm("O túnel está ativo. Deseja parar e fechar?", "Confirmar"))
             {
                 _tunnelService.StopAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();
             }
             else
             {
                 return;
             }
        }
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _timer.Stop();
        base.OnClosed(e);
    }
}
