using System.Windows;

namespace DevTools.Host.Wpf.Components;

public partial class ActionBarControl : System.Windows.Controls.UserControl
{
    public ActionBarControl()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty ShowNewProperty =
        DependencyProperty.Register(
            nameof(ShowNew),
            typeof(bool),
            typeof(ActionBarControl),
            new PropertyMetadata(true));

    public static readonly DependencyProperty ShowSaveProperty =
        DependencyProperty.Register(
            nameof(ShowSave),
            typeof(bool),
            typeof(ActionBarControl),
            new PropertyMetadata(true));

    public static readonly DependencyProperty ShowDeleteProperty =
        DependencyProperty.Register(
            nameof(ShowDelete),
            typeof(bool),
            typeof(ActionBarControl),
            new PropertyMetadata(true));

    public static readonly DependencyProperty ShowCancelProperty =
        DependencyProperty.Register(
            nameof(ShowCancel),
            typeof(bool),
            typeof(ActionBarControl),
            new PropertyMetadata(true));

    public static readonly DependencyProperty CanNewProperty =
        DependencyProperty.Register(
            nameof(CanNew),
            typeof(bool),
            typeof(ActionBarControl),
            new PropertyMetadata(true));

    public static readonly DependencyProperty CanSaveProperty =
        DependencyProperty.Register(
            nameof(CanSave),
            typeof(bool),
            typeof(ActionBarControl),
            new PropertyMetadata(true));

    public static readonly DependencyProperty CanDeleteProperty =
        DependencyProperty.Register(
            nameof(CanDelete),
            typeof(bool),
            typeof(ActionBarControl),
            new PropertyMetadata(true));

    public static readonly DependencyProperty CanCancelProperty =
        DependencyProperty.Register(
            nameof(CanCancel),
            typeof(bool),
            typeof(ActionBarControl),
            new PropertyMetadata(true));

    public bool ShowNew
    {
        get => (bool)GetValue(ShowNewProperty);
        set => SetValue(ShowNewProperty, value);
    }

    public bool ShowSave
    {
        get => (bool)GetValue(ShowSaveProperty);
        set => SetValue(ShowSaveProperty, value);
    }

    public bool ShowDelete
    {
        get => (bool)GetValue(ShowDeleteProperty);
        set => SetValue(ShowDeleteProperty, value);
    }

    public bool ShowCancel
    {
        get => (bool)GetValue(ShowCancelProperty);
        set => SetValue(ShowCancelProperty, value);
    }

    public bool CanNew
    {
        get => (bool)GetValue(CanNewProperty);
        set => SetValue(CanNewProperty, value);
    }

    public bool CanSave
    {
        get => (bool)GetValue(CanSaveProperty);
        set => SetValue(CanSaveProperty, value);
    }

    public bool CanDelete
    {
        get => (bool)GetValue(CanDeleteProperty);
        set => SetValue(CanDeleteProperty, value);
    }

    public bool CanCancel
    {
        get => (bool)GetValue(CanCancelProperty);
        set => SetValue(CanCancelProperty, value);
    }

    public event RoutedEventHandler? ActionNew;
    public event RoutedEventHandler? ActionSave;
    public event RoutedEventHandler? ActionDelete;
    public event RoutedEventHandler? ActionCancel;

    private void ActionNew_Click(object sender, RoutedEventArgs e)
        => ActionNew?.Invoke(this, e);

    private void ActionSave_Click(object sender, RoutedEventArgs e)
        => ActionSave?.Invoke(this, e);

    private void ActionDelete_Click(object sender, RoutedEventArgs e)
        => ActionDelete?.Invoke(this, e);

    private void ActionCancel_Click(object sender, RoutedEventArgs e)
        => ActionCancel?.Invoke(this, e);
}


