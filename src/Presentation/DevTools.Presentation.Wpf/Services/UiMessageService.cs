using DevTools.Presentation.Wpf.Views;
using System;
using System.Linq;
using System.Windows;

namespace DevTools.Presentation.Wpf.Services;

public static class UiMessageService
{
    public static Func<DevToolsDialogType, string, string, string?, string?, bool?>? DialogOverrideForTests { get; set; }
    public static Func<string, string, bool>? ConfirmOverrideForTests { get; set; }

    public static void ShowError(string message, string title = "Erro", Exception? ex = null)
    {
        if (ex != null)
        {
            AppLogger.Error($"{title}: {message}", ex);
        }

        ShowDialog(DevToolsDialogType.Error, title, message);
    }

    public static void ShowInfo(string message, string title = "Informacao")
    {
        ShowDialog(DevToolsDialogType.Info, title, message);
    }

    public static void ShowWarning(string message, string title = "Atencao")
    {
        ShowDialog(DevToolsDialogType.Warning, title, message);
    }

    public static bool Confirm(string message, string title = "Confirmar")
    {
        if (ConfirmOverrideForTests != null)
        {
            return ConfirmOverrideForTests(message, title);
        }

        return ShowDialog(DevToolsDialogType.Confirm, title, message, "Sim", "Nao");
    }

    public static bool ShowCustom(
        string message,
        string title,
        DevToolsDialogType type,
        string? primaryButtonText = null,
        string? secondaryButtonText = null)
    {
        return ShowDialog(type, title, message, primaryButtonText, secondaryButtonText);
    }

    private static bool ShowDialog(
        DevToolsDialogType type,
        string title,
        string message,
        string? primaryButtonText = null,
        string? secondaryButtonText = null)
    {
        if (DialogOverrideForTests != null)
        {
            var overridden = DialogOverrideForTests(type, title, message, primaryButtonText, secondaryButtonText);
            if (overridden.HasValue)
            {
                return overridden.Value;
            }
        }

        return RunOnUiThread(() =>
        {
            var dialog = new DevToolsDialogWindow(title, message, type, primaryButtonText, secondaryButtonText);
            var owner = GetActiveWindow();

            if (owner != null)
            {
                dialog.Owner = owner;
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            else
            {
                dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            var result = dialog.ShowDialog();
            return result == true;
        });
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
