using System;
using System.Threading.Tasks;
using System.Windows;
using DevTools.Core.Configuration;
using DevTools.Presentation.Wpf.Services;

namespace DevTools.Presentation.Wpf;

public partial class App : Application
{
    private JobManager _jobManager = null!;
    private SettingsService _settingsService = null!;
    private ConfigService _configService = null!;
    private ProfileManager _profileManager = null!;
    private TrayService _trayService = null!;

    public App()
    {
        SetupExceptionHandling();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        AppLogger.Info("=== Application Started ===");
        base.OnStartup(e);

        // Bootstrap manual (Pure DI)
        _jobManager = new JobManager();
        _settingsService = new SettingsService();
        _configService = new ConfigService();
        _profileManager = new ProfileManager();
        _trayService = new TrayService(_jobManager, _settingsService, _configService, _profileManager);

        // Instancia Dashboard (Hub)
        var dashboard = new DevTools.Presentation.Wpf.Views.DashboardWindow(_trayService, _jobManager, _configService);
        _trayService.SetDashboardWindow(dashboard);

        _trayService.Initialize();
        
        // Iniciar com a Dashboard aberta
        _trayService.ShowDashboard();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        AppLogger.Info("=== Application Exiting ===");
        _trayService?.Dispose();
        base.OnExit(e);
    }

    private void SetupExceptionHandling()
    {
        // UI Thread Exceptions
        DispatcherUnhandledException += (s, e) =>
        {
            AppLogger.Error("Unhandled UI Exception", e.Exception);
            // Optional: Prevent crash? 
            // e.Handled = true; 
            // For now, let's just log. If user wants to prevent crash, we can add that.
        };

        // Background Task Exceptions
        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            AppLogger.Error("Unobserved Task Exception", e.Exception);
            e.SetObserved(); // Prevent crash
        };

        // Current Domain Exceptions (Final backstop)
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            var ex = e.ExceptionObject as Exception;
            AppLogger.Error($"AppDomain Unhandled Exception (Terminating: {e.IsTerminating})", ex);
        };
    }
}