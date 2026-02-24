using System.Windows;

namespace DevTools.Presentation.Wpf.Components;

public partial class DevToolsFormSection : System.Windows.Controls.UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(DevToolsFormSection), new PropertyMetadata(string.Empty));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public DevToolsFormSection()
    {
        InitializeComponent();
    }
}
