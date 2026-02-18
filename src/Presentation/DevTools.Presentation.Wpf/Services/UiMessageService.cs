using System;
using System.Windows;

namespace DevTools.Presentation.Wpf.Services;

public static class UiMessageService
{
    public static void ShowError(string message, string title = "Erro", Exception? ex = null)
    {
        if (ex != null)
        {
            AppLogger.Error($"{title}: {message}", ex);
        }

        System.Windows.MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    public static void ShowInfo(string message, string title = "Informação")
    {
        System.Windows.MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public static bool Confirm(string message, string title = "Confirmar")
    {
        var mb = System.Windows.MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning);
        return mb == MessageBoxResult.Yes;
    }
}
