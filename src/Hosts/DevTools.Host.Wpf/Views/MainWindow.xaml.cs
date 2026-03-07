using System.Windows;
using DevTools.Infrastructure.Persistence;
using DevTools.Infrastructure.Persistence.Repositories;
using DevTools.Snapshot.Engine;
using DevTools.Snapshot.Services;

namespace DevTools.Host.Wpf.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        InitializeSnapshotWorkspace();
    }

    private void SnapshotNavButton_Click(object sender, RoutedEventArgs e)
    {
        SnapshotWorkspace.Visibility = Visibility.Visible;
    }

    private void InitializeSnapshotWorkspace()
    {
        var pathProvider = new SqlitePathProvider();
        var optionsFactory = new SqliteDbContextOptionsFactory(pathProvider);
        var bootstrapper = new SqliteBootstrapper(pathProvider, optionsFactory);
        bootstrapper.Migrate();

        var snapshotRepository = new SnapshotEntityRepository(optionsFactory.Create());
        var snapshotService = new SnapshotEntityService(snapshotRepository);
        var snapshotEngine = new SnapshotEngine();

        SnapshotWorkspace.Initialize(snapshotService, snapshotEngine);
    }
}
