using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DevTools.Presentation.Wpf.Services;

public static class UiMessageService
{
    private enum DialogType
    {
        Info,
        Warning,
        Error,
        Confirm
    }

    public static void ShowError(string message, string title = "Erro", Exception? ex = null)
    {
        if (ex != null)
        {
            AppLogger.Error($"{title}: {message}", ex);
        }

        if (TryShowDialogHost(DialogType.Error, title, message, out _))
        {
            return;
        }

        ShowFallbackDialog(DialogType.Error, title, message);
    }

    public static void ShowInfo(string message, string title = "Informacao")
    {
        if (TryShowDialogHost(DialogType.Info, title, message, out _))
        {
            return;
        }

        ShowFallbackDialog(DialogType.Info, title, message);
    }

    public static bool Confirm(string message, string title = "Confirmar")
    {
        if (TryShowDialogHost(DialogType.Confirm, title, message, out var hostResult))
        {
            return hostResult;
        }

        return ShowFallbackConfirmDialog(title, message);
    }

    public static void ShowWarning(string message, string title = "Atencao")
    {
        if (TryShowDialogHost(DialogType.Warning, title, message, out _))
        {
            return;
        }

        ShowFallbackDialog(DialogType.Warning, title, message);
    }

    private static bool TryShowDialogHost(DialogType type, string title, string message, out bool confirmResult)
    {
        confirmResult = false;

        try
        {
            var content = BuildDialogContent(type, title, message, closeByHost: true);
            var result = MaterialDesignThemes.Wpf.DialogHost.Show(content, "RootDialog").GetAwaiter().GetResult();
            confirmResult = result is bool b && b;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void ShowFallbackDialog(DialogType type, string title, string message)
    {
        RunOnUiThread(() =>
        {
            var dialog = BuildFallbackWindow(type, title, message);
            dialog.ShowDialog();
        });
    }

    private static bool ShowFallbackConfirmDialog(string title, string message)
    {
        return RunOnUiThread(() =>
        {
            var dialog = BuildFallbackWindow(DialogType.Confirm, title, message);
            var result = dialog.ShowDialog();
            return result == true;
        });
    }

    private static FrameworkElement BuildDialogContent(DialogType type, string title, string message, bool closeByHost)
    {
        var palette = ResolvePalette(type);

        var panel = new StackPanel
        {
            MinWidth = 500,
            MaxWidth = 640
        };

        panel.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            Foreground = System.Windows.Media.Brushes.White,
            Margin = new Thickness(0, 0, 0, 8)
        });

        panel.Children.Add(new TextBlock
        {
            Text = message,
            Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(210, 210, 210)),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 18)
        });

        panel.Children.Add(BuildButtonsRow(type, closeByHost, closeByWindow: null));

        return new Border
        {
            Padding = new Thickness(20),
            Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 48)),
            CornerRadius = new CornerRadius(8),
            BorderBrush = new SolidColorBrush(palette.BorderColor),
            BorderThickness = new Thickness(1),
            Child = panel
        };
    }

    private static Window BuildFallbackWindow(DialogType type, string title, string message)
    {
        var palette = ResolvePalette(type);

        var window = new Window
        {
            Title = title,
            Width = 620,
            SizeToContent = SizeToContent.Height,
            MinHeight = 180,
            MaxHeight = 420,
            WindowStyle = WindowStyle.None,
            ResizeMode = ResizeMode.NoResize,
            AllowsTransparency = true,
            Background = System.Windows.Media.Brushes.Transparent,
            ShowInTaskbar = false,
            Topmost = true,
            Owner = GetActiveWindow(),
            WindowStartupLocation = GetActiveWindow() != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen
        };

        var root = new Border
        {
            Padding = new Thickness(20),
            Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 48)),
            CornerRadius = new CornerRadius(8),
            BorderBrush = new SolidColorBrush(palette.BorderColor),
            BorderThickness = new Thickness(1)
        };

        var panel = new StackPanel();
        panel.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            Foreground = System.Windows.Media.Brushes.White,
            Margin = new Thickness(0, 0, 0, 8)
        });

        panel.Children.Add(new TextBlock
        {
            Text = message,
            Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(210, 210, 210)),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 18)
        });

        panel.Children.Add(BuildButtonsRow(type, closeByHost: false, closeByWindow: window));
        root.Child = panel;
        window.Content = root;

        return window;
    }

    private static StackPanel BuildButtonsRow(DialogType type, bool closeByHost, Window? closeByWindow)
    {
        var row = new StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right
        };

        void CloseDialog(object? payload = null)
        {
            if (closeByHost)
            {
                MaterialDesignThemes.Wpf.DialogHost.Close("RootDialog", payload);
                return;
            }

            if (closeByWindow == null)
            {
                return;
            }

            if (payload is bool boolPayload)
            {
                closeByWindow.DialogResult = boolPayload;
            }
            else
            {
                closeByWindow.DialogResult = true;
            }

            closeByWindow.Close();
        }

        if (type == DialogType.Confirm)
        {
            var cancelButton = CreateButton("Cancelar", isPrimary: false);
            cancelButton.Margin = new Thickness(0, 0, 10, 0);
            cancelButton.Click += (_, _) => CloseDialog(false);

            var confirmButton = CreateButton("Confirmar", isPrimary: true);
            confirmButton.Click += (_, _) => CloseDialog(true);

            row.Children.Add(cancelButton);
            row.Children.Add(confirmButton);
            return row;
        }

        var okButton = CreateButton("OK", isPrimary: true);
        okButton.Click += (_, _) => CloseDialog(null);
        row.Children.Add(okButton);

        return row;
    }

    private static System.Windows.Controls.Button CreateButton(string text, bool isPrimary)
    {
        return new System.Windows.Controls.Button
        {
            Content = text,
            MinWidth = 120,
            Height = 36,
            Padding = new Thickness(14, 0, 14, 0),
            BorderThickness = new Thickness(1),
            Background = isPrimary
                ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 122, 204))
                : System.Windows.Media.Brushes.Transparent,
            BorderBrush = isPrimary
                ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 122, 204))
                : new SolidColorBrush(System.Windows.Media.Color.FromRgb(90, 90, 90)),
            Foreground = System.Windows.Media.Brushes.White
        };
    }

    private static (System.Windows.Media.Color BorderColor, System.Windows.Media.Color AccentColor) ResolvePalette(DialogType type)
    {
        return type switch
        {
            DialogType.Error => (System.Windows.Media.Color.FromRgb(232, 17, 35), System.Windows.Media.Color.FromRgb(232, 17, 35)),
            DialogType.Warning => (System.Windows.Media.Color.FromRgb(255, 185, 0), System.Windows.Media.Color.FromRgb(255, 185, 0)),
            DialogType.Confirm => (System.Windows.Media.Color.FromRgb(90, 90, 90), System.Windows.Media.Color.FromRgb(0, 122, 204)),
            _ => (System.Windows.Media.Color.FromRgb(90, 90, 90), System.Windows.Media.Color.FromRgb(0, 122, 204))
        };
    }

    private static Window? GetActiveWindow()
    {
        var app = System.Windows.Application.Current;
        if (app == null)
        {
            return null;
        }

        return app.Windows
            .OfType<Window>()
            .FirstOrDefault(w => w.IsActive)
            ?? app.MainWindow;
    }

    private static void RunOnUiThread(Action action)
    {
        var app = System.Windows.Application.Current;
        if (app?.Dispatcher == null)
        {
            action();
            return;
        }

        if (app.Dispatcher.CheckAccess())
        {
            action();
            return;
        }

        app.Dispatcher.Invoke(action);
    }

    private static T RunOnUiThread<T>(Func<T> action)
    {
        var app = System.Windows.Application.Current;
        if (app?.Dispatcher == null)
        {
            return action();
        }

        if (app.Dispatcher.CheckAccess())
        {
            return action();
        }

        return app.Dispatcher.Invoke(action);
    }
}
