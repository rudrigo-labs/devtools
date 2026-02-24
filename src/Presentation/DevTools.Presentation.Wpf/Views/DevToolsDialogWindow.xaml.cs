using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DevTools.Presentation.Wpf.Views;

public enum DevToolsDialogResult
{
    None = 0,
    OK = 1,
    Cancel = 2,
    Yes = 6,
    No = 7
}

public partial class DevToolsDialogWindow : Window
{
    public DevToolsDialogResult DialogResultValue { get; private set; } = DevToolsDialogResult.None;

    public DevToolsDialogWindow()
    {
        InitializeComponent();
    }

    public static DevToolsDialogResult Show(string message, string title, MessageBoxButton buttons, MessageBoxImage icon)
    {
        var dialog = new DevToolsDialogWindow
        {
            TitleText = { Text = title },
            MessageText = { Text = message }
        };

        dialog.ConfigureButtons(buttons);
        dialog.ConfigureIcon(icon);

        dialog.ShowDialog();
        return dialog.DialogResultValue;
    }

    private void ConfigureButtons(MessageBoxButton buttons)
    {
        switch (buttons)
        {
            case MessageBoxButton.OK:
                OkButton.Visibility = Visibility.Visible;
                OkButton.IsDefault = true;
                break;
            case MessageBoxButton.OKCancel:
                OkButton.Visibility = Visibility.Visible;
                CancelButton.Visibility = Visibility.Visible;
                OkButton.IsDefault = true;
                CancelButton.IsCancel = true;
                break;
            case MessageBoxButton.YesNo:
                YesButton.Visibility = Visibility.Visible;
                NoButton.Visibility = Visibility.Visible;
                YesButton.IsDefault = true;
                NoButton.IsCancel = true;
                break;
            case MessageBoxButton.YesNoCancel:
                YesButton.Visibility = Visibility.Visible;
                NoButton.Visibility = Visibility.Visible;
                CancelButton.Visibility = Visibility.Visible;
                YesButton.IsDefault = true;
                CancelButton.IsCancel = true;
                break;
        }
    }

    private void ConfigureIcon(MessageBoxImage icon)
    {
        // Simple icon mapping (using colors/shapes or resources)
        // In a real app, use vector icons or images from resources
        switch (icon)
        {
            case MessageBoxImage.Error:
                IconPath.Data = Geometry.Parse("M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-2h2v2zm0-4h-2V7h2v6z");
                IconPath.Fill = System.Windows.Media.Brushes.Red;
                break;
            case MessageBoxImage.Warning:
                IconPath.Data = Geometry.Parse("M1 21h22L12 2 1 21zm12-3h-2v-2h2v2zm0-4h-2v-4h2v4z");
                IconPath.Fill = System.Windows.Media.Brushes.Orange;
                break;
            case MessageBoxImage.Question:
                IconPath.Data = Geometry.Parse("M11 18h2v-2h-2v2zm1-16C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 18c-4.41 0-8-3.59-8-8s3.59-8 8-8 8 3.59 8 8-3.59 8-8 8zm0-14c-2.21 0-4 1.79-4 4h2c0-1.1.9-2 2-2s2 .9 2 2c0 2-3 1.75-3 5h2c0-2.25 3-2.5 3-5 0-2.21-1.79-4-4-4z");
                IconPath.Fill = System.Windows.Media.Brushes.DodgerBlue; // Or primary color
                break;
            case MessageBoxImage.Information:
            default:
                IconPath.Data = Geometry.Parse("M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-6h2v6zm0-8h-2V7h2v2z");
                IconPath.Fill = System.Windows.Media.Brushes.LightBlue;
                break;
        }
    }

    private void Yes_Click(object sender, RoutedEventArgs e)
    {
        DialogResultValue = DevToolsDialogResult.Yes;
        Close();
    }

    private void No_Click(object sender, RoutedEventArgs e)
    {
        DialogResultValue = DevToolsDialogResult.No;
        Close();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        DialogResultValue = DevToolsDialogResult.OK;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResultValue = DevToolsDialogResult.Cancel;
        Close();
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }
}
