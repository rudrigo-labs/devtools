using DevTools.Core.Abstractions;
using DevTools.Core.Models;
using DevTools.Core.Utilities;
using DevTools.Harvest.Engine;
using DevTools.Harvest.Repositories;
using DevTools.Harvest.Services;
using DevTools.Host.Wpf.Configuration;
using DevTools.Host.Wpf.Facades;
using DevTools.Host.Wpf.Views;
using DevTools.Image.Engine;
using DevTools.Infrastructure.Persistence;
using DevTools.Infrastructure.Persistence.Repositories;
using DevTools.Migrations.Engine;
using DevTools.Migrations.Repositories;
using DevTools.Migrations.Services;
using DevTools.Ngrok.Engine;
using DevTools.Ngrok.Repositories;
using DevTools.Ngrok.Services;
using DevTools.Organizer.Engine;
using DevTools.Rename.Engine;
using DevTools.SearchText.Engine;
using DevTools.Snapshot.Engine;
using DevTools.Snapshot.Repositories;
using DevTools.Snapshot.Services;
using DevTools.SSHTunnel.Engine;
using DevTools.SSHTunnel.Repositories;
using DevTools.SSHTunnel.Services;
using DevTools.Utf8Convert.Engine;
using DevTools.Notes.Providers;
using DevTools.Notes.Repositories;
using DevTools.Notes.Services;
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

        // Utilitários de processo
        services.AddSingleton<IProcessRunner, SystemProcessRunner>();

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

        // ImageSplit
        services.AddSingleton<ImageSplitEngine>();
        services.AddSingleton<IImageSplitFacade, ImageSplitFacade>();

        // SearchText
        services.AddSingleton<SearchTextEngine>();
        services.AddSingleton<ISearchTextFacade, SearchTextFacade>();

        // Organizer
        services.AddSingleton<OrganizerEngine>();
        services.AddSingleton<IOrganizerFacade, OrganizerFacade>();

        // Utf8Convert
        services.AddSingleton<Utf8ConvertEngine>();
        services.AddSingleton<IUtf8ConvertFacade, Utf8ConvertFacade>();

        // Migrations
        services.AddSingleton<IMigrationsEntityRepository, MigrationsEntityRepository>();
        services.AddSingleton<MigrationsEntityService>();
        services.AddSingleton<MigrationsEngine>();
        services.AddSingleton<IMigrationsFacade, MigrationsFacade>();

        // SSHTunnel
        services.AddSingleton<ISshTunnelEntityRepository, SshTunnelEntityRepository>();
        services.AddSingleton<SshTunnelEntityService>();
        services.AddSingleton<SshTunnelEngine>();
        services.AddSingleton<ISshTunnelFacade, SshTunnelFacade>();

        // Ngrok
        services.AddSingleton<INgrokEntityRepository, NgrokEntityRepository>();
        services.AddSingleton<NgrokEntityService>();
        services.AddSingleton<NgrokEngine>();
        services.AddSingleton<INgrokFacade, NgrokFacade>();

        // Host WPF
        services.AddSingleton<RenameWorkspaceView>();
        services.AddSingleton<SnapshotWorkspaceView>();
        services.AddSingleton<HarvestWorkspaceView>();
        services.AddSingleton<ImageSplitWorkspaceView>();
        services.AddSingleton<SearchTextWorkspaceView>();
        services.AddSingleton<OrganizerWorkspaceView>();
        services.AddSingleton<Utf8ConvertWorkspaceView>();
        services.AddSingleton<MigrationsWorkspaceView>();
        services.AddSingleton<SshTunnelWorkspaceView>();
        services.AddSingleton<NgrokWorkspaceView>();
        // Notes
        services.AddSingleton<INotesEntityRepository, NotesEntityRepository>();
        services.AddSingleton<NotesEntityService>();
        services.AddSingleton<GoogleDriveAuthService>();
        services.AddSingleton<INotesFacade, NotesFacade>();
        services.AddSingleton<NotesWorkspaceView>();
        services.AddSingleton<MainWindow>();
    }
}
