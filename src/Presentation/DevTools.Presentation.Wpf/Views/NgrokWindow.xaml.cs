using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.Generic;
using DevTools.Core.Models;
using DevTools.Ngrok.Engine;
using DevTools.Ngrok.Models;
using DevTools.Presentation.Wpf.Services;

namespace DevTools.Presentation.Wpf.Views;

public partial class NgrokWindow : Window
{
    private readonly JobManager _jobManager;
    private readonly SettingsService _settingsService;
    private readonly NgrokEngine _engine;
    
    // Timer para auto-refresh
    private readonly System.Windows.Threading.DispatcherTimer _timer;

    public NgrokWindow(JobManager jobManager, SettingsService settingsService)
    {
        InitializeComponent();
        _jobManager = jobManager;
        _settingsService = settingsService;
        _engine = new NgrokEngine();

        ProfileSelector.GetOptionsFunc = GetCurrentOptions;
        ProfileSelector.ProfileLoaded += LoadProfile;

        LoadPosition();
        Closing += (s, e) => SavePosition();

        _timer = new System.Windows.Threading.DispatcherTimer();
        _timer.Interval = TimeSpan.FromSeconds(3);
        _timer.Tick += async (s, e) => await RefreshTunnels();
        
        Loaded += async (s, e) => 
        {
            await RefreshTunnels();
            _timer.Start();
        };
    }

    private Dictionary<string, string> GetCurrentOptions()
    {
        var options = new Dictionary<string, string>();
        options["port"] = PortInput.Text;
        return options;
    }

    private void LoadProfile(ToolProfile profile)
    {
        if (profile.Options.TryGetValue("port", out var port)) PortInput.Text = port;
    }

    private async Task RefreshTunnels()
    {
        try
        {
            var request = new NgrokRequest(NgrokAction.ListTunnels);
            var result = await _engine.ExecuteAsync(request);

            if (result.IsSuccess && result.Value?.Tunnels != null)
            {
                var tunnels = result.Value.Tunnels;
                TunnelsList.ItemsSource = tunnels;
                EmptyStateText.Visibility = tunnels.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                StatusText.Text = $"Atualizado em: {DateTime.Now:HH:mm:ss}";
            }
            else
            {
                // Ngrok pode não estar rodando
                TunnelsList.ItemsSource = null;
                EmptyStateText.Visibility = Visibility.Visible;
                StatusText.Text = "Ngrok inativo ou API inacessível";
            }
        }
        catch
        {
            // Ignora erros de conexão silenciosamente no timer
        }
    }

    private async void StartButton_Click(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(PortInput.Text, out int port))
        {
            StartButton.IsEnabled = false;
            StartButton.Content = "Iniciando...";

            var request = new NgrokRequest(
                NgrokAction.StartHttp, 
                StartOptions: new NgrokStartOptions("http", port)
            );

            await Task.Run(async () => 
            {
                var result = await _engine.ExecuteAsync(request);
                
                Dispatcher.Invoke(() => 
                {
                    StartButton.IsEnabled = true;
                    StartButton.Content = "Expor Porta";
                    
                    if (!result.IsSuccess)
                    {
                        var errorSummary = string.Join(", ", result.Errors.Select(x => x.Message));
                        UiMessageService.ShowError($"Falha ao iniciar: {errorSummary}", "Erro ao iniciar Ngrok");
                    }
                    else
                    {
                        // Aguarda um pouco para a API subir e atualiza a lista
                        Task.Delay(1000).ContinueWith(_ => Dispatcher.Invoke(RefreshTunnels));
                    }
                });
            });
        }
        else
        {
            UiMessageService.ShowError("Porta inválida!", "Erro");
        }
    }

    private async void KillAll_Click(object sender, RoutedEventArgs e)
    {
        if (UiMessageService.Confirm("Isso fechará TODOS os túneis Ngrok abertos. Continuar?", "Confirmar"))
        {
            var request = new NgrokRequest(NgrokAction.KillAll);
            await _engine.ExecuteAsync(request);
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
            // Feedback visual rápido seria legal, mas tooltip serve
        }
    }

    private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
    {
        e.Handled = new Regex("[^0-9]+").IsMatch(e.Text);
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // DragMove();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void LoadPosition()
    {
        /* Position handled by TrayService
        if (_settingsService.Settings.NgrokWindowTop.HasValue && _settingsService.Settings.NgrokWindowLeft.HasValue)
        {
            Top = _settingsService.Settings.NgrokWindowTop.Value;
            Left = _settingsService.Settings.NgrokWindowLeft.Value;
        }
        else
        {
            var workArea = SystemParameters.WorkArea;
            Left = workArea.Right - Width - 20;
            Top = workArea.Bottom - Height - 20;
        }
        */
    }

    private void SavePosition()
    {
        _settingsService.Settings.NgrokWindowTop = Top;
        _settingsService.Settings.NgrokWindowLeft = Left;
        _settingsService.Save();
    }
}
