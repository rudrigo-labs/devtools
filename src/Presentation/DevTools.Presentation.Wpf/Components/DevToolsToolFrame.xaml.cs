using System.Windows;
using System.Windows.Input;

namespace DevTools.Presentation.Wpf.Components;

public partial class DevToolsToolFrame : System.Windows.Controls.UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(DevToolsToolFrame), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty HelpTextProperty =
        DependencyProperty.Register(nameof(HelpText), typeof(string), typeof(DevToolsToolFrame), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty StatusTextProperty =
        DependencyProperty.Register(nameof(StatusText), typeof(string), typeof(DevToolsToolFrame), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty PrimaryButtonContentProperty =
        DependencyProperty.Register(nameof(PrimaryButtonContent), typeof(object), typeof(DevToolsToolFrame), new PropertyMetadata("OK"));

    public static readonly DependencyProperty SecondaryButtonContentProperty =
        DependencyProperty.Register(nameof(SecondaryButtonContent), typeof(object), typeof(DevToolsToolFrame), new PropertyMetadata("Cancelar"));

    public static readonly DependencyProperty PrimaryCommandProperty =
        DependencyProperty.Register(nameof(PrimaryCommand), typeof(ICommand), typeof(DevToolsToolFrame), new PropertyMetadata(null));

    public static readonly DependencyProperty SecondaryCommandProperty =
        DependencyProperty.Register(nameof(SecondaryCommand), typeof(ICommand), typeof(DevToolsToolFrame), new PropertyMetadata(null));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string HelpText
    {
        get => (string)GetValue(HelpTextProperty);
        set => SetValue(HelpTextProperty, value);
    }

    public string StatusText
    {
        get => (string)GetValue(StatusTextProperty);
        set => SetValue(StatusTextProperty, value);
    }

    public object PrimaryButtonContent
    {
        get => GetValue(PrimaryButtonContentProperty);
        set => SetValue(PrimaryButtonContentProperty, value);
    }

    public object SecondaryButtonContent
    {
        get => GetValue(SecondaryButtonContentProperty);
        set => SetValue(SecondaryButtonContentProperty, value);
    }

    public ICommand? PrimaryCommand
    {
        get => (ICommand?)GetValue(PrimaryCommandProperty);
        set => SetValue(PrimaryCommandProperty, value);
    }

    public ICommand? SecondaryCommand
    {
        get => (ICommand?)GetValue(SecondaryCommandProperty);
        set => SetValue(SecondaryCommandProperty, value);
    }

    public DevToolsToolFrame()
    {
        InitializeComponent();
    }
}

