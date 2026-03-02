using System.Windows;
using DevTools.Core.Configuration;
using DevTools.Presentation.Wpf.Services;

namespace DevTools.Presentation.Wpf.Views;

public partial class DashboardWindow : Window
{
    private readonly TrayService _trayService;
    private readonly JobManager _jobManager;
    private readonly ConfigService _configService;

    public DashboardWindow(TrayService trayService, JobManager jobManager, ConfigService configService)
    {
        InitializeComponent();
        _trayService = trayService;
        _jobManager = jobManager;
        _configService = configService;

        JobsDataGrid.ItemsSource = _jobManager.Jobs;
        
        Loaded += DashboardWindow_Loaded;
    }

    private void DashboardWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Position at bottom-right
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - 20; // 20px margin
        Top = workArea.Bottom - Height - 20; // 20px margin
    }

    private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            if (WindowState == WindowState.Maximized)
                WindowState = WindowState.Normal;
            else
                WindowState = WindowState.Maximized;
        }
        else
        {
            DragMove();
        }
    }

    public void ResetToHome()
    {
        MainTabControl.SelectedItem = TabTools;
    }

    public void SelectJobsTab()
    {
        MainTabControl.SelectedItem = TabJobs;
    }

    private void NavButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button btn || btn.Tag is not string tag)
            return;

        switch (tag)
        {
            case "Tools":
                MainTabControl.SelectedItem = TabTools;
                break;
            case "Jobs":
                MainTabControl.SelectedItem = TabJobs;
                break;
            case "Settings":
                MainTabControl.SelectedItem = TabSettings;
                break;
        }
    }

    private void OpenTool_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button btn || btn.Tag is not string toolTag)
            return;

        _trayService.OpenTool(toolTag);
    }

    private void Shutdown_Click(object sender, RoutedEventArgs e)
    {
        if (System.Windows.MessageBox.Show("Deseja realmente encerrar a aplicação? As tarefas em segundo plano serão interrompidas.", 
            "Encerrar DevTools", 
            MessageBoxButton.YesNo, 
            MessageBoxImage.Warning) == MessageBoxResult.Yes)
        {
            System.Windows.Application.Current.Shutdown();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }
}
