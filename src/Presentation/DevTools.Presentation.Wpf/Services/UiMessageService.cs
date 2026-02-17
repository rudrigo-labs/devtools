namespace DevTools.Presentation.Wpf.Services;

public static class UiMessageService
{
    public static void ShowError(string message, string title = "Erro", System.Exception? ex = null)
    {
        if (ex != null)
        {
            AppLogger.Error($"{title}: {message}", ex);
        }

        System.Windows.MessageBox.Show(message, title, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
    }

    public static void ShowInfo(string message, string title = "Informação")
    {
        System.Windows.MessageBox.Show(message, title, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    public static bool Confirm(string message, string title = "Confirmar")
    {
        var result = System.Windows.MessageBox.Show(message, title, System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
        return result == System.Windows.MessageBoxResult.Yes;
    }
}
