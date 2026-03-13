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

    public static readonly DependencyProperty NewTextProperty =
        DependencyProperty.Register(
            nameof(NewText),
            typeof(string),
            typeof(ActionBarControl),
            new PropertyMetadata("Novo"));

    public static readonly DependencyProperty SaveTextProperty =
        DependencyProperty.Register(
            nameof(SaveText),
            typeof(string),
            typeof(ActionBarControl),
            new PropertyMetadata("Salvar"));

    public static readonly DependencyProperty DeleteTextProperty =
        DependencyProperty.Register(
            nameof(DeleteText),
            typeof(string),
            typeof(ActionBarControl),
            new PropertyMetadata("Remover"));

    public static readonly DependencyProperty CancelTextProperty =
        DependencyProperty.Register(
            nameof(CancelText),
            typeof(string),
            typeof(ActionBarControl),
            new PropertyMetadata("Cancelar"));

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

    public static readonly DependencyProperty ShowHelpProperty =
        DependencyProperty.Register(
            nameof(ShowHelp),
            typeof(bool),
            typeof(ActionBarControl),
            new PropertyMetadata(false));

    public static readonly DependencyProperty CanHelpProperty =
        DependencyProperty.Register(
            nameof(CanHelp),
            typeof(bool),
            typeof(ActionBarControl),
            new PropertyMetadata(true));

    public static readonly DependencyProperty HelpTextProperty =
        DependencyProperty.Register(
            nameof(HelpText),
            typeof(string),
            typeof(ActionBarControl),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty HelpTextLabelProperty =
        DependencyProperty.Register(
            nameof(HelpTextLabel),
            typeof(string),
            typeof(ActionBarControl),
            new PropertyMetadata("Ajuda"));

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

    public string NewText
    {
        get => (string)GetValue(NewTextProperty);
        set => SetValue(NewTextProperty, value);
    }

    public string SaveText
    {
        get => (string)GetValue(SaveTextProperty);
        set => SetValue(SaveTextProperty, value);
    }

    public string DeleteText
    {
        get => (string)GetValue(DeleteTextProperty);
        set => SetValue(DeleteTextProperty, value);
    }

    public string CancelText
    {
        get => (string)GetValue(CancelTextProperty);
        set => SetValue(CancelTextProperty, value);
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

    public bool ShowHelp
    {
        get => (bool)GetValue(ShowHelpProperty);
        set => SetValue(ShowHelpProperty, value);
    }

    public bool CanHelp
    {
        get => (bool)GetValue(CanHelpProperty);
        set => SetValue(CanHelpProperty, value);
    }

    public string HelpText
    {
        get => (string)GetValue(HelpTextProperty);
        set => SetValue(HelpTextProperty, value);
    }

    public string HelpTextLabel
    {
        get => (string)GetValue(HelpTextLabelProperty);
        set => SetValue(HelpTextLabelProperty, value);
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


