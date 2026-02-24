using System.Windows;
using DevTools.Presentation.Wpf.Views;

namespace DevTools.Presentation.Wpf.Utilities;

public static class DevToolsMessage
{
    public static void Info(string text, string? title = null)
        => DevToolsDialogWindow.Show(text, title ?? "Informação", MessageBoxButton.OK, MessageBoxImage.Information);

    public static void Warning(string text, string? title = null)
        => DevToolsDialogWindow.Show(text, title ?? "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);

    public static void Error(string text, string? title = null)
        => DevToolsDialogWindow.Show(text, title ?? "Erro", MessageBoxButton.OK, MessageBoxImage.Error);

    public static bool Confirm(string text, string? title = null)
        => DevToolsDialogWindow.Show(text, title ?? "Confirmação", MessageBoxButton.YesNo, MessageBoxImage.Question) == DevToolsDialogResult.Yes;
}
