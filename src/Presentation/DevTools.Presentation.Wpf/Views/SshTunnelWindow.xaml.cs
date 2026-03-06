using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using DevTools.Core.Configuration;
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
    private readonly SshKeyService _sshKeyService;
    private readonly bool _ownsTunnelService;

    public SshTunnelWindow(
        JobManager jobManager,
        SettingsService settingsService,
        ConfigService configService,
        ToolConfigurationManager toolConfigurationManager,
        TunnelService? sharedTunnelService = null)
    {
        InitializeComponent();
        _jobManager = jobManager;
        _settingsService = settingsService;
        _configService = configService;

        var processRunner = new SystemProcessRunner();
        _tunnelService = sharedTunnelService ?? new TunnelService(processRunner);
        _sshKeyService = new SshKeyService(processRunner);
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
        if (!string.IsNullOrWhiteSpace(_settingsService.Settings.LastSshStrictHostKeyChecking))
        {
            var targetTag = _settingsService.Settings.LastSshStrictHostKeyChecking;
            foreach (var item in StrictHostKeyCheckingCombo.Items.OfType<ComboBoxItem>())
            {
                if (string.Equals(item.Tag?.ToString(), targetTag, StringComparison.OrdinalIgnoreCase))
                {
                    StrictHostKeyCheckingCombo.SelectedItem = item;
                    break;
                }
            }
        }

        ConnectTimeoutInput.Text = _settingsService.Settings.LastSshConnectTimeoutSeconds?.ToString() ?? string.Empty;
    }

    private async void ToggleTunnel_Click(object sender, RoutedEventArgs e)
    {
        var primaryButton = MainFrame.PrimaryButton;
        if (primaryButton == null) return;

        if (_tunnelService.IsOn)
        {
            primaryButton.IsEnabled = false;
            primaryButton.Content = "Parando...";
            SetSshStatus("Parando...", "DevToolsStatusError");
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

        var configuration = BuildConfigurationFromUi();

        var strictTag = (StrictHostKeyCheckingCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Default";
        _settingsService.Settings.LastSshStrictHostKeyChecking = strictTag;
        _settingsService.Settings.LastSshConnectTimeoutSeconds = configuration.ConnectTimeoutSeconds;
        _settingsService.Save();

        primaryButton.IsEnabled = false;
        primaryButton.Content = "Conectando...";
        SetSshStatus("Conectando...", "DevToolsAccent");

        try
        {
            await _tunnelService.StartAsync(configuration);
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

    private TunnelConfiguration BuildConfigurationFromUi()
    {
        _ = int.TryParse(SshPortInput.Text, out var sshPort);
        _ = int.TryParse(LocalPortInput.Text, out var localPort);
        _ = int.TryParse(RemotePortInput.Text, out var remotePort);

        return new TunnelConfiguration
        {
            Name = BuildTunnelName(),
            SshHost = SshHostInput.Text,
            SshPort = sshPort > 0 ? sshPort : 22,
            SshUser = SshUserInput.Text,
            IdentityFile = IdentityFileSelector.SelectedPath ?? string.Empty,
            LocalBindHost = LocalBindInput.Text,
            LocalPort = localPort,
            RemoteHost = RemoteHostInput.Text,
            RemotePort = remotePort,
            StrictHostKeyChecking = ParseStrictHostKeyChecking(),
            ConnectTimeoutSeconds = ParseOptionalPositiveInt(ConnectTimeoutInput.Text)
        };
    }

    private string BuildTunnelName()
    {
        var host = string.IsNullOrWhiteSpace(SshHostInput.Text) ? "host" : SshHostInput.Text.Trim();
        var local = string.IsNullOrWhiteSpace(LocalPortInput.Text) ? "local" : LocalPortInput.Text.Trim();
        var remoteHost = string.IsNullOrWhiteSpace(RemoteHostInput.Text) ? "remote" : RemoteHostInput.Text.Trim();
        var remotePort = string.IsNullOrWhiteSpace(RemotePortInput.Text) ? "remote" : RemotePortInput.Text.Trim();
        return $"{host}:{local}->{remoteHost}:{remotePort}";
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

        if (!string.IsNullOrWhiteSpace(ConnectTimeoutInput.Text)
            && (!int.TryParse(ConnectTimeoutInput.Text, out var timeout) || timeout <= 0))
        {
            errorMessage = "Connect Timeout deve ser um número inteiro maior que zero.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    private SshStrictHostKeyChecking ParseStrictHostKeyChecking()
    {
        var selectedTag = (StrictHostKeyCheckingCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        return selectedTag switch
        {
            "Yes" => SshStrictHostKeyChecking.Yes,
            "No" => SshStrictHostKeyChecking.No,
            "AcceptNew" => SshStrictHostKeyChecking.AcceptNew,
            _ => SshStrictHostKeyChecking.Default
        };
    }

    private static int? ParseOptionalPositiveInt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return int.TryParse(value, out var parsed) && parsed > 0 ? parsed : null;
    }

    private void UpdateStatusUI()
    {
        var primaryButton = MainFrame.PrimaryButton;
        if (primaryButton == null) return;

        if (_tunnelService.IsOn)
        {
            SetSshStatus("Conectado", "DevToolsStatusOk");
            primaryButton.Content = "Desconectar";
            primaryButton.Style = (Style)FindResource("SecondaryButtonStyle");
            return;
        }

        SetSshStatus("Parado", "DevToolsStatusIdle");
        primaryButton.Content = "Conectar";
        primaryButton.Style = (Style)FindResource("PrimaryButtonStyle");
    }

    private void SetSshStatus(string text, string badgeBrushKey)
    {
        if (SshStatusText != null)
        {
            SshStatusText.Text = text;
        }

        if (SshStatusBadge != null && TryFindResource(badgeBrushKey) is System.Windows.Media.Brush brush)
        {
            SshStatusBadge.Background = brush;
        }
    }

    private void SavePosition()
    {
        // Sem persistencia de posicao no momento.
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void GenerateKeyButton_Click(object sender, RoutedEventArgs e)
    {
        if (GenerateKeyButton == null)
        {
            return;
        }

        GenerateKeyButton.IsEnabled = false;
        var oldContent = GenerateKeyButton.Content;
        GenerateKeyButton.Content = "Gerando...";

        try
        {
            var result = await _sshKeyService.GenerateAsync();
            IdentityFileSelector.SelectedPath = result.PrivateKeyPath;
            UiMessageService.ShowInfo(
                $"Chave SSH gerada com sucesso.\n\nPrivada: {result.PrivateKeyPath}\nPublica: {result.PublicKeyPath}",
                "Chave SSH criada");
        }
        catch (SshTunnelException ex)
        {
            UiMessageService.ShowError(ex.Message, "Erro ao gerar chave SSH", ex);
        }
        catch (Exception ex)
        {
            UiMessageService.ShowError("Falha inesperada ao gerar chave SSH.", "Erro ao gerar chave SSH", ex);
        }
        finally
        {
            GenerateKeyButton.IsEnabled = true;
            GenerateKeyButton.Content = oldContent;
        }
    }
}



