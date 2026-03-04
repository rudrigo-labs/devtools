using System;
using System.Windows;

namespace DevTools.Tests;

internal static class TestWpfApplication
{
    private static readonly Uri DarkThemeUri = new(
        "/DevTools.Presentation.Wpf;component/Theme/DarkTheme.xaml",
        UriKind.Relative);

    public static void EnsureInitialized()
    {
        var app = Application.Current;
        if (app == null)
        {
            try
            {
                app = new Application
                {
                    ShutdownMode = ShutdownMode.OnExplicitShutdown
                };
            }
            catch (InvalidOperationException)
            {
                // Some test hosts can keep the WPF app-domain flag as initialized.
                app = Application.Current;
            }
        }

        if (app == null)
        {
            return;
        }

        if (app.Resources.Contains("ModernWindowStyle"))
        {
            return;
        }

        try
        {
            var darkTheme = (ResourceDictionary)Application.LoadComponent(DarkThemeUri);
            app.Resources = darkTheme;
        }
        catch
        {
            // Fallback: tests will fail naturally if critical resources are missing.
        }
    }
}
