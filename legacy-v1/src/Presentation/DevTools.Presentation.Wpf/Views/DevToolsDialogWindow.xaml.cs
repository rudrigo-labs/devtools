using MaterialDesignThemes.Wpf;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace DevTools.Presentation.Wpf.Views;

public enum DevToolsDialogType
{
    Info,
    Warning,
    Error,
    Confirm
}

public partial class DevToolsDialogWindow : Window
{
    private readonly DevToolsDialogType _type;
    private readonly string _primaryButtonText;
    private readonly string? _secondaryButtonText;

    public DevToolsDialogWindow(
        string title,
        string message,
        DevToolsDialogType type,
        string? primaryButtonText = null,
        string? secondaryButtonText = null)
    {
        InitializeComponent();
        _type = type;
        _primaryButtonText = primaryButtonText ?? string.Empty;
        _secondaryButtonText = secondaryButtonText;

        DialogTitleText.Text = title;
        DialogMessageText.Text = message;

        ConfigureByType(type);
        Loaded += (_, _) => PrimaryButton.Focus();
    }

    private void ConfigureByType(DevToolsDialogType type)
    {
        var accent = ResolveTypeBrush(type);
        TypeAccentBar.Background = accent;
        TypeIcon.Foreground = accent;

        if (type == DevToolsDialogType.Confirm)
        {
            TypeIcon.Kind = PackIconKind.HelpCircleOutline;
            PrimaryButton.Content = string.IsNullOrWhiteSpace(_primaryButtonText) ? "Sim" : _primaryButtonText;
            SecondaryButton.Content = string.IsNullOrWhiteSpace(_secondaryButtonText) ? "Nao" : _secondaryButtonText;
            SecondaryButton.Visibility = Visibility.Visible;
            return;
        }

        PrimaryButton.Content = string.IsNullOrWhiteSpace(_primaryButtonText) ? "OK" : _primaryButtonText;
        if (!string.IsNullOrWhiteSpace(_secondaryButtonText))
        {
            SecondaryButton.Content = _secondaryButtonText;
            SecondaryButton.Visibility = Visibility.Visible;
        }
        else
        {
            SecondaryButton.Visibility = Visibility.Collapsed;
        }

        TypeIcon.Kind = type switch
        {
            DevToolsDialogType.Warning => PackIconKind.AlertOutline,
            DevToolsDialogType.Error => PackIconKind.AlertCircleOutline,
            _ => PackIconKind.InformationOutline
        };
    }

    private System.Windows.Media.Brush ResolveTypeBrush(DevToolsDialogType type)
    {
        return type switch
        {
            DevToolsDialogType.Warning => new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 185, 0)),
            DevToolsDialogType.Error => TryFindResource("ErrorBrush") as System.Windows.Media.Brush ?? new SolidColorBrush(System.Windows.Media.Color.FromRgb(232, 17, 35)),
            DevToolsDialogType.Confirm => TryFindResource("DevToolsAccent") as System.Windows.Media.Brush ?? new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 122, 204)),
            _ => TryFindResource("DevToolsAccent") as System.Windows.Media.Brush ?? new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 122, 204))
        };
    }

    private void PrimaryButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void SecondaryButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            DialogResult = SecondaryButton.Visibility == Visibility.Visible ? false : true;
            Close();
            return;
        }

        if (e.Key == Key.Enter)
        {
            DialogResult = true;
            Close();
        }
    }
}
