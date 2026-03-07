using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace DevTools.Presentation.Wpf.Components;

public partial class InlineValidationAdorner : System.Windows.Controls.UserControl, INotifyPropertyChanged
{
    public static readonly DependencyProperty TargetProperty =
        DependencyProperty.Register(
            nameof(Target),
            typeof(FrameworkElement),
            typeof(InlineValidationAdorner),
            new PropertyMetadata(null, OnVisualStateDependencyPropertyChanged));

    public static readonly DependencyProperty IsInvalidProperty =
        DependencyProperty.Register(
            nameof(IsInvalid),
            typeof(bool),
            typeof(InlineValidationAdorner),
            new PropertyMetadata(false, OnVisualStateDependencyPropertyChanged));

    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(
            nameof(Message),
            typeof(string),
            typeof(InlineValidationAdorner),
            new PropertyMetadata(string.Empty, OnVisualStateDependencyPropertyChanged));

    public static readonly DependencyProperty ShowAsteriskProperty =
        DependencyProperty.Register(
            nameof(ShowAsterisk),
            typeof(bool),
            typeof(InlineValidationAdorner),
            new PropertyMetadata(true, OnVisualStateDependencyPropertyChanged));

    public FrameworkElement? Target
    {
        get => (FrameworkElement?)GetValue(TargetProperty);
        set => SetValue(TargetProperty, value);
    }

    public bool IsInvalid
    {
        get => (bool)GetValue(IsInvalidProperty);
        set => SetValue(IsInvalidProperty, value);
    }

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public bool ShowAsterisk
    {
        get => (bool)GetValue(ShowAsteriskProperty);
        set => SetValue(ShowAsteriskProperty, value);
    }

    public bool IsAsteriskVisible => IsInvalid && ShowAsterisk && Target != null;
    public bool IsMessageVisible => IsInvalid && !string.IsNullOrWhiteSpace(Message) && Target != null;

    public InlineValidationAdorner()
    {
        InitializeComponent();
        DataContext = this;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private static void OnVisualStateDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not InlineValidationAdorner component)
            return;

        component.NotifyVisualStateChanged();
    }

    private void NotifyVisualStateChanged()
    {
        OnPropertyChanged(nameof(IsAsteriskVisible));
        OnPropertyChanged(nameof(IsMessageVisible));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
