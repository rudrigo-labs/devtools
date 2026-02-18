using System;
using System.Windows;
using WpfControls = System.Windows.Controls;

namespace DevTools.Presentation.Wpf.Views;

public partial class ToolsTabView : WpfControls.UserControl
{
    public event EventHandler<string>? ToolRequested;

    public ToolsTabView()
    {
        InitializeComponent();
    }

    private void ToolButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is WpfControls.Button btn && btn.CommandParameter is string toolTag)
        {
            ToolRequested?.Invoke(this, toolTag);
        }
    }
}
