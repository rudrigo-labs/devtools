using System.Windows;
using System.Windows.Controls;
using DevTools.Ngrok.Engine;
using DevTools.Ngrok.Models;
using DevTools.Presentation.Wpf.Services;
using DevTools.Presentation.Wpf.Utilities;
using DevTools.Core.Models;

namespace DevTools.Presentation.Wpf.Views;

public partial class NgrokWindow : Window
{
    private readonly SettingsService _settingsService;

    public NgrokRequest? Result { get; private set; }

    public NgrokWindow(SettingsService settingsService)
    {
        InitializeComponent();
        _settingsService = settingsService;

        if (_settingsService.Settings.LastNgrokPort.HasValue)
            PortBox.Text = _settingsService.Settings.LastNgrokPort.Value.ToString();
        if (!string.IsNullOrEmpty(_settingsService.Settings.LastNgrokSubdomain))
            SubdomainBox.Text = _settingsService.Settings.LastNgrokSubdomain;
        if (!string.IsNullOrEmpty(_settingsService.Settings.LastNgrokTunnelName))
            TunnelNameBox.Text = _settingsService.Settings.LastNgrokTunnelName;
        if (!string.IsNullOrEmpty(_settingsService.Settings.LastNgrokAuthToken))
            AuthTokenBox.Text = _settingsService.Settings.LastNgrokAuthToken;

        ProfileSelector.GetOptionsFunc = GetCurrentOptions;
        ProfileSelector.ProfileLoaded += LoadProfile;

        // Position handled by TrayService logic
    }

    private Dictionary<string, string> GetCurrentOptions()
    {
        var actionItem = ActionCombo.SelectedItem as ComboBoxItem;
        var action = actionItem?.Tag?.ToString() ?? "ListTunnels";

        return new Dictionary<string, string>
        {
            ["action"] = action,
            ["port"] = PortBox.Text,
            ["subdomain"] = SubdomainBox.Text,
            ["tunnel"] = TunnelNameBox.Text,
            ["auth"] = AuthTokenBox.Text
        };
    }

    private void LoadProfile(ToolProfile profile)
    {
        if (profile.Options.TryGetValue("action", out var action))
        {
            foreach (ComboBoxItem item in ActionCombo.Items)
            {
                if (item.Tag?.ToString() == action)
                {
                    ActionCombo.SelectedItem = item;
                    break;
                }
            }
        }
        
        if (profile.Options.TryGetValue("port", out var port)) PortBox.Text = port;
        if (profile.Options.TryGetValue("subdomain", out var subdomain)) SubdomainBox.Text = subdomain;
        if (profile.Options.TryGetValue("tunnel", out var tunnel)) TunnelNameBox.Text = tunnel;
        if (profile.Options.TryGetValue("auth", out var auth)) AuthTokenBox.Text = auth;
    }

    private void ActionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Could toggle visibility here based on action
    }

    private async void Run_Click(object sender, RoutedEventArgs e)
    {
        var actionItem = ActionCombo.SelectedItem as ComboBoxItem;
        var actionTag = actionItem?.Tag?.ToString() ?? "ListTunnels";
        
        if (!Enum.TryParse<NgrokAction>(actionTag, out var action))
        {
            DevToolsMessage.Error("Ação inválida.", "Erro");
            return;
        }

        var extraArgs = new List<string>();
        if (!string.IsNullOrWhiteSpace(AuthTokenBox.Text))
        {
            extraArgs.Add("--authtoken");
            extraArgs.Add(AuthTokenBox.Text);
        }
        
        if (!string.IsNullOrWhiteSpace(SubdomainBox.Text))
        {
            extraArgs.Add("--subdomain");
            extraArgs.Add(SubdomainBox.Text);
        }

        int port = 8080;
        if (!int.TryParse(PortBox.Text, out var p))
        {
            DevToolsMessage.Error("Porta inválida. Digite um número.", "Erro de Validação");
            return;
        }
        port = p;

        _settingsService.Settings.LastNgrokPort = port;
        _settingsService.Settings.LastNgrokSubdomain = SubdomainBox.Text;
        _settingsService.Settings.LastNgrokTunnelName = TunnelNameBox.Text;
        _settingsService.Settings.LastNgrokAuthToken = AuthTokenBox.Text;
        _settingsService.Save();

        var startOptions = new NgrokStartOptions(
            Protocol: "http",
            Port: port,
            ExtraArgs: extraArgs
        );

        Result = new NgrokRequest(
            Action: action,
            TunnelName: TunnelNameBox.Text,
            StartOptions: startOptions
        );

        var engine = new NgrokEngine();
        
        IsEnabled = false;
        RunSummary.Clear();

        try
        {
            var result = await Task.Run(() => engine.ExecuteAsync(Result));
            RunSummary.BindResult(result);
        }
        catch (Exception ex)
        {
            AppLogger.Error("Erro crítico ao executar Ngrok", ex);
            DevToolsMessage.Error($"Erro crítico: {ex.Message}", "Erro");
        }
        finally
        {
            IsEnabled = true;
        }
    }

    private void Header_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        DragMove();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
