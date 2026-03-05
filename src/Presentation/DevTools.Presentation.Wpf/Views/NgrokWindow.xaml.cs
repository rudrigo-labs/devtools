using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using DevTools.Ngrok.Engine;
using DevTools.Ngrok.Models;
using DevTools.Ngrok.Services;
using DevTools.Presentation.Wpf.Services;

namespace DevTools.Presentation.Wpf.Views;

public partial class NgrokWindow : Window
{
    private readonly NgrokSetupService _setupService;
    private readonly NgrokOnboardingService _onboardingService;

    // Timer para auto-refresh
    private readonly System.Windows.Threading.DispatcherTimer _timer;
    private NgrokSettings _ngrokSettings = new();

    public NgrokWindow(SettingsService settingsService)
    {
        InitializeComponent();
        _setupService = new NgrokSetupService();
        _onboardingService = new NgrokOnboardingService();

        _timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };

        LoadPosition();
        Closing += (_, _) =>
        {
            SavePosition(settingsService);
            _timer.Stop();
        };
        _timer.Tick += async (_, _) => await RefreshTunnels();

        Loaded += async (_, _) =>
        {
            LoadNgrokSettings();
            UpdateOnboardingState();

            if (IsNgrokConfigured())
            {
                await RefreshTunnels();
                _timer.Start();
            }
        };
    }

    private void LoadNgrokSettings()
    {
        _ngrokSettings = _setupService.GetSettings();
        _ngrokSettings.Normalize();

        AuthTokenInput.Text = _ngrokSettings.AuthToken;
    }

    private bool IsNgrokConfigured()
    {
        return _setupService.IsConfigured();
    }

    private void UpdateOnboardingState()
    {
        var configured = IsNgrokConfigured();

        OnboardingPanel.Visibility = configured ? Visibility.Collapsed : Visibility.Visible;
        TunnelConfigPanel.Visibility = configured ? Visibility.Visible : Visibility.Collapsed;

        if (MainFrame.PrimaryButton != null)
            MainFrame.PrimaryButton.IsEnabled = configured;

        if (MainFrame.SecondaryButton != null)
            MainFrame.SecondaryButton.IsEnabled = configured;

        MainFrame.StatusText = configured
            ? "Pronto para iniciar tunel."
            : "Ngrok nao configurado.";
    }

    private async Task RefreshTunnels()
    {
        try
        {
            if (!IsNgrokConfigured())
            {
                TunnelsList.ItemsSource = null;
                EmptyStateText.Visibility = Visibility.Visible;
                MainFrame.StatusText = "Ngrok nao configurado.";
                return;
            }

            var result = await _setupService.ListTunnelsAsync();

            if (result.IsSuccess && result.Value?.Tunnels != null)
            {
                var tunnels = result.Value.Tunnels;
                TunnelsList.ItemsSource = tunnels;
                EmptyStateText.Visibility = tunnels.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                MainFrame.StatusText = $"Atualizado em: {DateTime.Now:HH:mm:ss}";
            }
            else
            {
                TunnelsList.ItemsSource = null;
                EmptyStateText.Visibility = Visibility.Visible;
                MainFrame.StatusText = "Ngrok inativo ou API inacessivel.";
            }
        }
        catch
        {
            // Ignora erros de conexao silenciosamente no timer
        }
    }

    private void SaveNgrokToken_Click(object sender, RoutedEventArgs e)
    {
        var token = (AuthTokenInput.Text ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(token))
        {
            ValidationUiService.ShowInline(MainFrame, "Campos obrigatorios: Authtoken do ngrok.");
            return;
        }

        ValidationUiService.ClearInline(MainFrame);
        _setupService.SaveAuthtoken(token);
        _ngrokSettings = _setupService.GetSettings();

        UiMessageService.ShowInfo("Authtoken salvo com sucesso.", "Ngrok");
        UpdateOnboardingState();

        _ = RefreshTunnels();
        _timer.Start();
    }

    private void CreateNgrokAccount_Click(object sender, RoutedEventArgs e)
    {
        OpenExternalLink(_onboardingService.GetSignupUrl());
    }

    private void OpenOnboardingHelp_Click(object sender, RoutedEventArgs e)
    {
        var helpWindow = new NgrokHelpWindow
        {
            Owner = this
        };

        helpWindow.ShowDialog();
    }

    private async void StartButton_Click(object sender, RoutedEventArgs e)
    {
        var startButton = MainFrame.PrimaryButton;
        if (startButton == null)
            return;

        if (!IsNgrokConfigured())
        {
            ValidationUiService.ShowInline(MainFrame, "Ngrok nao configurado. Informe e salve o Authtoken.");
            return;
        }

        if (string.IsNullOrWhiteSpace(PortInput.Text))
        {
            ValidationUiService.ShowInline(MainFrame, "Campos obrigatorios: Porta Local.");
            return;
        }

        ValidationUiService.ClearInline(MainFrame);

        if (int.TryParse(PortInput.Text, out var port) && port >= 1 && port <= 65535)
        {
            startButton.IsEnabled = false;
            startButton.Content = "Iniciando...";

            await Task.Run(async () =>
            {
                var result = await _setupService.StartTunnelAsync(port);

                Dispatcher.Invoke(() =>
                {
                    startButton.IsEnabled = true;
                    startButton.Content = "Iniciar Tunel";

                    if (!result.IsSuccess)
                    {
                        var errorSummary = string.Join(", ", result.Errors.Select(x => x.Message));
                        UiMessageService.ShowError($"Falha ao iniciar: {errorSummary}", "Erro ao iniciar Ngrok");
                    }
                    else
                    {
                        MainFrame.StatusText = "Tunel iniciado com sucesso.";
                        Task.Delay(1000).ContinueWith(_ => Dispatcher.Invoke(RefreshTunnels));
                    }
                });
            });
        }
        else
        {
            ValidationUiService.ShowInline(MainFrame, "Porta invalida. Informe um valor entre 1 e 65535.");
        }
    }

    private async void StopButton_Click(object sender, RoutedEventArgs e)
    {
        if (!IsNgrokConfigured())
            return;

        var result = await _setupService.StopTunnelAsync();
        MainFrame.StatusText = result.IsSuccess
            ? $"Tuneis finalizados: {result.Value?.Killed ?? 0}"
            : "Falha ao finalizar tuneis.";

        await RefreshTunnels();
    }

    private async void KillAll_Click(object sender, RoutedEventArgs e)
    {
        if (UiMessageService.Confirm("Isso fechara TODOS os tuneis Ngrok abertos. Continuar?", "Confirmar"))
        {
            var result = await _setupService.StopTunnelAsync();
            MainFrame.StatusText = result.IsSuccess
                ? $"Tuneis finalizados: {result.Value?.Killed ?? 0}"
                : "Falha ao finalizar tuneis.";
            await RefreshTunnels();
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshTunnels();
    }

    private void CopyUrl_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is string url)
        {
            System.Windows.Clipboard.SetText(url);
            MainFrame.StatusText = "URL copiada para a area de transferencia.";
        }
    }

    private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
    {
        e.Handled = new Regex("[^0-9]+$").IsMatch(e.Text);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private static void OpenExternalLink(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            UiMessageService.ShowError($"Nao foi possivel abrir o navegador.\n{ex.Message}", "Erro ao abrir link");
        }
    }

    private void LoadPosition()
    {
        // Position handled by TrayService
    }

    private void SavePosition(SettingsService settingsService)
    {
        settingsService.Settings.NgrokWindowTop = Top;
        settingsService.Settings.NgrokWindowLeft = Left;
        settingsService.Save();
    }
}
