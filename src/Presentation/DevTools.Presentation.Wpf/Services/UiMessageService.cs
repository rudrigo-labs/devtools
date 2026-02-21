using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DevTools.Presentation.Wpf.Services;

public static class UiMessageService
{
    public static void ShowError(string message, string title = "Erro", Exception? ex = null)
    {
        if (ex != null)
        {
            AppLogger.Error($"{title}: {message}", ex);
        }

        try
        {
            var panel = new StackPanel { Width = 360 };
            var titleBlock = new TextBlock
            {
                Text = title,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 80, 80)),
                Margin = new Thickness(0, 0, 0, 8)
            };
            var msgBlock = new TextBlock
            {
                Text = message,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 18)
            };
            var okButton = new System.Windows.Controls.Button
            {
                Content = "OK",
                Padding = new Thickness(14, 6, 14, 6),
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(232, 17, 35)),
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255)),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right
            };
            okButton.Click += (_, _) => MaterialDesignThemes.Wpf.DialogHost.Close("RootDialog");

            var buttons = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right
            };
            buttons.Children.Add(okButton);

            panel.Children.Add(titleBlock);
            panel.Children.Add(msgBlock);
            panel.Children.Add(buttons);

            var border = new Border
            {
                Padding = new Thickness(20),
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 48)),
                CornerRadius = new CornerRadius(8),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(232, 17, 35)),
                BorderThickness = new Thickness(1),
                Child = panel
            };

            MaterialDesignThemes.Wpf.DialogHost.Show(border, "RootDialog").GetAwaiter().GetResult();
            return;
        }
        catch
        {
        }

        System.Windows.MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    public static void ShowInfo(string message, string title = "Informação")
    {
        try
        {
            var panel = new StackPanel { Width = 320 };
            var titleBlock = new TextBlock
            {
                Text = title,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = System.Windows.Media.Brushes.White,
                Margin = new Thickness(0, 0, 0, 8)
            };
            var msgBlock = new TextBlock
            {
                Text = message,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(204, 204, 204)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 18)
            };
            var okButton = new System.Windows.Controls.Button
            {
                Content = "OK",
                Padding = new Thickness(14, 6, 14, 6),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right
            };
            okButton.Click += (_, _) => MaterialDesignThemes.Wpf.DialogHost.Close("RootDialog");

            var buttons = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right
            };
            buttons.Children.Add(okButton);

            panel.Children.Add(titleBlock);
            panel.Children.Add(msgBlock);
            panel.Children.Add(buttons);

            var border = new Border
            {
                Padding = new Thickness(20),
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 48)),
                CornerRadius = new CornerRadius(8),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(63, 63, 70)),
                BorderThickness = new Thickness(1),
                Child = panel
            };

            MaterialDesignThemes.Wpf.DialogHost.Show(border, "RootDialog").GetAwaiter().GetResult();
            return;
        }
        catch
        {
            // Fallback quando não existe um DialogHost com Identifier="RootDialog" no visual tree atual
        }

        System.Windows.MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public static bool Confirm(string message, string title = "Confirmar")
    {
        try
        {
            var panel = new StackPanel { Width = 360 };
            var titleBlock = new TextBlock
            {
                Text = title,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 204, 0)),
                Margin = new Thickness(0, 0, 0, 8)
            };
            var msgBlock = new TextBlock
            {
                Text = message,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 18)
            };

            var noButton = new System.Windows.Controls.Button
            {
                Content = "NÃO",
                Padding = new Thickness(14, 6, 14, 6)
            };
            noButton.Click += (_, _) => MaterialDesignThemes.Wpf.DialogHost.Close("RootDialog", false);

            var yesButton = new System.Windows.Controls.Button
            {
                Content = "SIM",
                Padding = new Thickness(14, 6, 14, 6),
                Margin = new Thickness(10, 0, 0, 0),
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 120, 215)),
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255))
            };
            yesButton.Click += (_, _) => MaterialDesignThemes.Wpf.DialogHost.Close("RootDialog", true);

            var buttons = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right
            };
            buttons.Children.Add(noButton);
            buttons.Children.Add(yesButton);

            panel.Children.Add(titleBlock);
            panel.Children.Add(msgBlock);
            panel.Children.Add(buttons);

            var border = new Border
            {
                Padding = new Thickness(20),
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 48)),
                CornerRadius = new CornerRadius(8),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(63, 63, 70)),
                BorderThickness = new Thickness(1),
                Child = panel
            };

            var result = MaterialDesignThemes.Wpf.DialogHost.Show(border, "RootDialog").GetAwaiter().GetResult();
            if (result is bool b)
            {
                return b;
            }

            return false;
        }
        catch
        {
        }

        var mb = System.Windows.MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning);
        return mb == MessageBoxResult.Yes;
    }
}
