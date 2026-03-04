using System;
using System.Collections.Generic;
using System.Windows;

namespace DevTools.Presentation.Wpf.ToolRouting;

public sealed class DetachedWindowLaunchStrategy : IToolLaunchStrategy
{
    private readonly Dictionary<string, Window> _singletonWindows = new(StringComparer.OrdinalIgnoreCase);
    private Window? _currentDetachedWindow;

    public ToolLaunchMode Mode => ToolLaunchMode.DetachedWindow;
    public bool HasOpenWindow => _currentDetachedWindow != null && _currentDetachedWindow.IsVisible;

    public void CloseCurrentWindow()
    {
        if (_currentDetachedWindow != null && _currentDetachedWindow.IsVisible)
        {
            _currentDetachedWindow.Close();
        }
    }

    public void Launch(ToolDescriptor descriptor, ToolLaunchContext context)
    {
        if (descriptor.Factory == null)
        {
            context.LogInfo?.Invoke($"Tool '{descriptor.Id}' has no factory configured for DetachedWindow.");
            return;
        }

        if (descriptor.Singleton && _singletonWindows.TryGetValue(descriptor.Id, out var existingWindow) && existingWindow.IsVisible)
        {
            BringToFront(existingWindow);
            _currentDetachedWindow = existingWindow;
            return;
        }

        if (_currentDetachedWindow != null && _currentDetachedWindow.IsVisible)
        {
            _currentDetachedWindow.Close();
            _currentDetachedWindow = null;
        }

        if (descriptor.Factory(context.Services) is not Window window)
        {
            context.LogInfo?.Invoke($"Tool '{descriptor.Id}' factory must return a Window for DetachedWindow mode.");
            return;
        }

        ConfigureWindow(window, context.GetMainWindow());
        window.Closed += (_, __) =>
        {
            if (_currentDetachedWindow == window)
            {
                _currentDetachedWindow = null;
            }

            _singletonWindows.Remove(descriptor.Id);
            var mainWindow = context.GetMainWindow();
            if (mainWindow != null)
            {
                mainWindow.IsEnabled = true;
                if (mainWindow.IsVisible)
                {
                    mainWindow.Activate();
                }
            }
        };

        if (descriptor.Singleton)
        {
            _singletonWindows[descriptor.Id] = window;
        }

        _currentDetachedWindow = window;
        window.Show();
    }

    private static void BringToFront(Window window)
    {
        if (window.WindowState == WindowState.Minimized)
        {
            window.WindowState = WindowState.Normal;
        }

        window.Activate();
        window.Focus();
    }

    private static void ConfigureWindow(Window window, Window? mainWindow)
    {
        if (mainWindow != null && window != mainWindow)
        {
            window.Owner = mainWindow;
            window.ShowInTaskbar = false;
            window.WindowStartupLocation = WindowStartupLocation.Manual;
        }

        window.Loaded += (_, __) =>
        {
            var screen = SystemParameters.WorkArea;
            window.Left = screen.Right - window.ActualWidth - 20;
            window.Top = screen.Bottom - window.ActualHeight - 20;
        };
    }
}

