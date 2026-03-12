using DevTools.Core.Models;
using DevTools.Harvest.Engine;
using DevTools.Harvest.Repositories;
using DevTools.Harvest.Services;
using DevTools.Host.Wpf.Configuration;
using DevTools.Host.Wpf.Facades;
using DevTools.Host.Wpf.Views;
using DevTools.Infrastructure.Persistence;
using DevTools.Infrastructure.Persistence.Repositories;
using DevTools.Rename.Engine;
using DevTools.Snapshot.Engine;
using DevTools.Snapshot.Repositories;
using DevTools.Snapshot.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DevTools.Host.Wpf;

public partial class App : System.Windows.Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);

        var appSettings = AppSettingsLoader.Load();

        var services = new ServiceCollection();
        ConfigureServices(services, appSettings);
        _serviceProvider = services.BuildServiceProvider();

        try
        {
            _serviceProvider.GetRequiredService<SqliteBootstrapper>().Migrate();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Falha ao inicializar banco SQLite: {ex.Message}",
                "DevTools - Erro de inicializacao",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);

            Shutdown(-1);
            return;
        }

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        _serviceProvider = null;
        base.OnExit(e);
    }

    private static void ConfigureServices(IServiceCollection services, AppSettings appSettings)
    {
        // Configuração estática da aplicação
        services.AddSingleton(appSettings);

        // Infraestrutura — DbContextOptions resolvido uma única vez via factory.
        services.AddSingleton<SqlitePathProvider>();
        services.AddSingleton<SqliteDbContextOptionsFactory>();
        services.AddSingleton(sp =>
            sp.GetRequiredService<SqliteDbContextOptionsFactory>().Create());
        services.AddSingleton<SqliteBootstrapper>();

        // Snapshot
        services.AddSingleton<ISnapshotEntityRepository, SnapshotEntityRepository>();
        services.AddSingleton<SnapshotEntityService>();
        services.AddSingleton<SnapshotEngine>();
        services.AddSingleton<ISnapshotFacade, SnapshotFacade>();

        // Rename
        services.AddSingleton<RenameEngine>();
        services.AddSingleton<IRenameFacade, RenameFacade>();

        // Harvest
        services.AddSingleton<IHarvestEntityRepository, HarvestEntityRepository>();
        services.AddSingleton<HarvestEntityService>();
        services.AddSingleton<HarvestEngine>();
        services.AddSingleton<IHarvestFacade, HarvestFacade>();

        // Host WPF
        services.AddSingleton<RenameWorkspaceView>();
        services.AddSingleton<SnapshotWorkspaceView>();
        services.AddSingleton<HarvestWorkspaceView>();
        services.AddSingleton<MainWindow>();
    }
}
