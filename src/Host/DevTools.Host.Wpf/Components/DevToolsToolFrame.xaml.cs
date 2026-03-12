using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DevTools.Host.Wpf.Components;

public class DevToolsToolFrame : ContentControl
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

    public event RoutedEventHandler? PrimaryButtonClick;
    public event RoutedEventHandler? SecondaryButtonClick;
    public event RoutedEventHandler? CloseButtonClick;

    public System.Windows.Controls.Button? PrimaryButton { get; private set; }
    public System.Windows.Controls.Button? SecondaryButton { get; private set; }

    static DevToolsToolFrame()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(DevToolsToolFrame), new FrameworkPropertyMetadata(typeof(DevToolsToolFrame)));
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (GetTemplateChild("PART_PrimaryButton") is System.Windows.Controls.Button primaryBtn)
        {
            PrimaryButton = primaryBtn;
            primaryBtn.Click += (s, e) => PrimaryButtonClick?.Invoke(this, e);
        }

        if (GetTemplateChild("PART_SecondaryButton") is System.Windows.Controls.Button secondaryBtn)
        {
            SecondaryButton = secondaryBtn;
            secondaryBtn.Click += (s, e) => SecondaryButtonClick?.Invoke(this, e);
        }

        if (GetTemplateChild("PART_CloseButton") is System.Windows.Controls.Button closeBtn)
        {
            closeBtn.Click += (s, e) => 
            {
                CloseButtonClick?.Invoke(this, e);
                Window.GetWindow(this)?.Close();
            };
        }

        if (GetTemplateChild("PART_Header") is Border header)
        {
            header.MouseLeftButtonDown += (s, e) => Window.GetWindow(this)?.DragMove();
        }
    }
}


