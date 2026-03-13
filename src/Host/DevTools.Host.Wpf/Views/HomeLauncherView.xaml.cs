using System.Windows;

namespace DevTools.Host.Wpf.Views;

public partial class HomeLauncherView : System.Windows.Controls.UserControl
{
    public event Action<string>? OpenToolRequested;

    public HomeLauncherView()
    {
        InitializeComponent();
    }

    private void OpenTool_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button btn)
            return;

        if (btn.Tag is not string toolTag || string.IsNullOrWhiteSpace(toolTag))
            return;

        OpenToolRequested?.Invoke(toolTag);
    }
}
