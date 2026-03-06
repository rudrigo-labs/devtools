using System;
using System.Threading.Tasks;
using System.Windows;
using DevTools.Core.Configuration;
using DevTools.Presentation.Wpf.Persistence;
using DevTools.Presentation.Wpf.Persistence.Stores;
using DevTools.Presentation.Wpf.Services;
using Microsoft.EntityFrameworkCore;

namespace DevTools.Presentation.Wpf;

public partial class App : System.Windows.Application
{
    private JobManager _jobManager = null!;
    private SettingsService _settingsService = null!;
    private ConfigService _configService = null!;
    private ToolConfigurationManager _toolConfigurationManager = null!;
    private TrayService _trayService = null!;
    private GoogleDriveService _googleDriveService = null!;
    private SqliteBootstrapper _sqliteBootstrapper = null!;
    private StorageBackend _storageBackend;

    public App()
    {
        SetupExceptionHandling();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        AppLogger.Info("=== Application Started ===");
        base.OnStartup(e);

        // Bootstrap manual (Pure DI)
        _storageBackend = StorageBackendResolver.Resolve();
        _jobManager = new JobManager();
        _settingsService = new SettingsService();
        _configService = new ConfigService();
        _sqliteBootstrapper = new SqliteBootstrapper(new SqlitePathProvider());
        TryInitializeSqlite();
        _toolConfigurationManager = CreateToolConfigurationManager();
        _googleDriveService = new GoogleDriveService();

        var toolConfigurationUIService = new ToolConfigurationUIService(_toolConfigurationManager);
        _trayService = new TrayService(_jobManager, _settingsService, _configService, _toolConfigurationManager, _googleDriveService);

        // Instancia MainWindow (Hub)
        var mainWindow = new DevTools.Presentation.Wpf.Views.MainWindow(_trayService, _jobManager, _configService, toolConfigurationUIService, _googleDriveService);
        _trayService.SetMainWindow(mainWindow);

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

    private void TryInitializeSqlite()
    {
        if (_storageBackend != StorageBackend.Sqlite)
        {
            return;
        }

        try
        {
            _sqliteBootstrapper.EnsureDatabase();
        }
        catch (Exception ex)
        {
            // Fallback seguro: a aplicacao continua no modo legado baseado em JSON.
            AppLogger.Error("SQLite bootstrap failed. Continuing with legacy JSON persistence.", ex);
        }
    }

    private ToolConfigurationManager CreateToolConfigurationManager()
    {
        if (_storageBackend != StorageBackend.Sqlite)
        {
            return new ToolConfigurationManager();
        }

        try
        {
            var pathProvider = new SqlitePathProvider();
            var dbOptions = new DbContextOptionsBuilder<DevToolsDbContext>()
                .UseSqlite(pathProvider.GetConnectionString())
                .Options;

            var configurationStore = new SqliteToolConfigurationStore(dbOptions);
            return new ToolConfigurationManager(configurationStore);
        }
        catch (Exception ex)
        {
            AppLogger.Error("Failed to initialize SQLite configuration store. Falling back to JSON configuration store.", ex);
            return new ToolConfigurationManager();
        }
    }
}


