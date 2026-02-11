using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using DevTools.Presentation.Wpf.Services;

namespace DevTools.Presentation.Wpf.Components;

public partial class ConfigMissingOverlay : UserControl
{
    private readonly ConfigService _configService;

    public ConfigMissingOverlay()
    {
        InitializeComponent();
        // Fallback for design-time or manual usage
        _configService = new ConfigService();
        PathBox.Text = _configService.ConfigPath;
    }

    public ConfigMissingOverlay(ConfigService configService) : this()
    {
        _configService = configService;
        PathBox.Text = _configService.ConfigPath;
    }

    private void OpenConfig_Click(object sender, RoutedEventArgs e)
    {
        _configService.CreateDefaultIfNotExists();
        
        var path = _configService.ConfigPath;
        if (File.Exists(path))
        {
            Process.Start("explorer.exe", $"/select,\"{path}\"");
        }
    }
}
