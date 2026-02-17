using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using H.NotifyIcon;
using System.Drawing; // SystemIcons fallback
using System.IO;
using DevTools.Presentation.Wpf.Views;
using DevTools.Presentation.Wpf.Utilities;
using DevTools.Core.Configuration;

namespace DevTools.Presentation.Wpf.Services;

public class TrayService : IDisposable
{
    private readonly JobManager _jobManager;
    private readonly SettingsService _settingsService;
    private readonly ConfigService _configService;
    private readonly ProfileManager _profileManager;

    private TaskbarIcon _taskbarIcon = null!;
    private Window? _jobCenterWindow;
    private NotesWindow? _notesWindow;
    private DashboardWindow? _dashboardWindow;

    // Tool Windows References
    private Window? _currentToolWindow;
    private OrganizerWindow? _organizerWindow;
    private ImageSplitWindow? _imageSplitWindow;
    private RenameWindow? _renameWindow;
    private Utf8ConvertWindow? _utf8ConvertWindow;
    private SnapshotWindow? _snapshotWindow;
    private SshTunnelWindow? _sshTunnelWindow;
    private NgrokWindow? _ngrokWindow;
    private SearchTextWindow? _searchTextWindow;
    private MigrationsWindow? _migrationsWindow;
    private HarvestWindow? _harvestWindow;
    private LogsWindow? _logsWindow;

    public void SetDashboardWindow(DashboardWindow dashboardWindow)
    {
        _dashboardWindow = dashboardWindow;
    }

    public bool HasOpenToolWindow => _currentToolWindow != null && _currentToolWindow.IsVisible;

    // Helper to enforce Single Instance and Bottom-Right Positioning
    private void ShowWindow<T>(Func<T?> getWindow, Action<T?> setWindow, Func<T> factory) where T : Window
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            var window = getWindow();

            // If the requested window is already open, just activate it
            if (window != null && window.IsVisible)
            {
                window.Activate();
                window.Focus();
                if (window.WindowState == WindowState.Minimized)
                    window.WindowState = WindowState.Normal;
                
                _currentToolWindow = window;
                return;
            }

            // Close any other open tool window
            if (_currentToolWindow != null && _currentToolWindow.IsVisible)
            {
                _currentToolWindow.Close();
                _currentToolWindow = null;
            }

            // Create and show the new window
            window = factory();
            setWindow(window);
            _currentToolWindow = window;

            window.Closed += (_, __) => 
            {
                setWindow(null);
                if (_currentToolWindow == window)
                    _currentToolWindow = null;
            };

            // Enforce Bottom-Right Positioning
            window.Loaded += (s, e) =>
            {
                 var screen = SystemParameters.WorkArea;
                 window.Left = screen.Right - window.ActualWidth - 20;
                 window.Top = screen.Bottom - window.ActualHeight - 20;
            };

            window.Show();
        });
    }

    public void OpenTool(string tag)
    {
        switch (tag)
        {
            case "Notes": ShowNotesWindow(); break;
            case "Organizer": ShowOrganizerWindow(); break;
            case "Harvest": OnHarvestClick(this, new RoutedEventArgs()); break;
            case "SearchText": ShowSearchTextWindow(); break;
            case "Migrations": ShowMigrationsWindow(); break;
            case "ImageSplitter": ShowImageSplitWindow(); break;
            case "Rename": ShowRenameWindow(); break;
            case "Utf8Convert": ShowUtf8Window(); break;
            case "Snapshot": ShowSnapshotWindow(); break;
            case "SSHTunnel": ShowSshTunnelWindow(); break;
            case "Ngrok": ShowNgrokWindow(); break;
            case "Jobs": ShowJobCenter(); break;
            case "Logs": ShowLogsWindow(); break;
        }
    }

    public void ShowDashboard()
    {
        if (_dashboardWindow != null)
        {
            _dashboardWindow.ResetToHome();
            _dashboardWindow.Show();
            _dashboardWindow.Activate();
            if (_dashboardWindow.WindowState == WindowState.Minimized)
                _dashboardWindow.WindowState = WindowState.Normal;
        }
    }

    public TrayService(JobManager jobManager, SettingsService settingsService, ConfigService configService, ProfileManager profileManager)
    {
        _jobManager = jobManager;
        _settingsService = settingsService;
        _configService = configService;
        _profileManager = profileManager;
        _jobManager.OnJobCompleted += OnJobCompleted;
    }

    public void Initialize()
    {
        _taskbarIcon = new TaskbarIcon
        {
            ToolTipText = "DevTools Tray",
            ContextMenu = CreateContextMenu(),
            DoubleClickCommand = new DelegateCommand(ShowDashboard)
        };

        if (!TrySetIconSource())
        {
            // fallback se falhar carregamento do recurso
            _taskbarIcon.Icon = SystemIcons.Application;
        }

        _taskbarIcon.ForceCreate();
    }

    private bool TrySetIconSource()
    {
        try
        {
            // 1) Tenta carregar via pack URI relativo ao assembly atual (robusto após publish)
            var uriSimple = new Uri("pack://application:,,,/Assets/app.ico");
            var streamInfo = System.Windows.Application.GetResourceStream(uriSimple);
            if (streamInfo != null)
            {
                _taskbarIcon.Icon = new Icon(streamInfo.Stream);
                return true;
            }

            // 2) Fallback: tenta via assembly qualificado (alguns ambientes preferem o formato antigo)
            var uriQualified = new Uri("pack://application:,,,/DevTools.Presentation.Wpf;component/Assets/app.ico");
            var streamInfo2 = System.Windows.Application.GetResourceStream(uriQualified);
            if (streamInfo2 != null)
            {
                _taskbarIcon.Icon = new Icon(streamInfo2.Stream);
                return true;
            }

            // 3) Fallback: arquivo físico no diretório de deploy (dotnet publish/Inno Setup)
            var candidatePath = Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
            if (File.Exists(candidatePath))
            {
                _taskbarIcon.Icon = new Icon(candidatePath);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            AppLogger.Error("Error loading icon", ex);
            System.Diagnostics.Debug.WriteLine($"Error loading icon: {ex.Message}");
            return false;
        }
    }

    private System.Windows.Controls.ContextMenu CreateContextMenu()
    {
        // Carrega o menu estilizado dos recursos (TrayResources.xaml)
        if (System.Windows.Application.Current.FindResource("TrayContextMenu") is System.Windows.Controls.ContextMenu menu)
        {
            foreach (var item in menu.Items)
            {
                if (item is System.Windows.Controls.MenuItem menuItem && menuItem.Tag is string tag)
                {
                    switch (tag)
                    {
                        case "Dashboard":
                            menuItem.Click += (s, e) => ShowDashboard();
                            break;

                        case "Notes":
                            menuItem.Click += (s, e) => ShowNotesWindow();
                            break;

                        case "Organizer":
                            menuItem.Click += (s, e) => ShowOrganizerWindow();
                            break;

                        case "Harvest":
                            menuItem.Click += OnHarvestClick;
                            break;

                        case "SearchText":
                            menuItem.Click += (s, e) => ShowSearchTextWindow();
                            break;

                        case "Migrations":
                            menuItem.Click += (s, e) => ShowMigrationsWindow();
                            break;

                        case "ImageSplitter":
                            menuItem.Click += (s, e) => ShowImageSplitWindow();
                            break;

                        case "Rename":
                            menuItem.Click += (s, e) => ShowRenameWindow();
                            break;

                        case "Utf8Convert":
                            menuItem.Click += (s, e) => ShowUtf8Window();
                            break;

                        case "Snapshot":
                            menuItem.Click += (s, e) => ShowSnapshotWindow();
                            break;

                        case "SSHTunnel":
                            menuItem.Click += (s, e) => ShowSshTunnelWindow();
                            break;

                        case "Ngrok":
                            menuItem.Click += (s, e) => ShowNgrokWindow();
                            break;

                        case "Jobs":
                            menuItem.Click += (s, e) => ShowJobCenter();
                            break;

                        case "Logs":
                            menuItem.Click += (s, e) => ShowLogsWindow();
                            break;

                        case "Exit":
                            menuItem.Click += (s, e) => System.Windows.Application.Current.Shutdown();
                            break;
                    }
                }
            }

            return menu;
        }

        // Fallback básico caso o recurso não seja encontrado
        var fallbackMenu = new System.Windows.Controls.ContextMenu();

        var jobs = new System.Windows.Controls.MenuItem { Header = "Jobs (Fallback)" };
        jobs.Click += (s, e) => ShowJobCenter();
        fallbackMenu.Items.Add(jobs);

        var itemExit = new System.Windows.Controls.MenuItem { Header = "Sair (Fallback)" };
        itemExit.Click += (s, e) => System.Windows.Application.Current.Shutdown();
        fallbackMenu.Items.Add(itemExit);

        return fallbackMenu;
    }

    private void ShowOrganizerWindow() => ShowWindow(() => _organizerWindow, w => _organizerWindow = w, () => new OrganizerWindow(_jobManager, _settingsService));

    private void ShowImageSplitWindow() => ShowWindow(() => _imageSplitWindow, w => _imageSplitWindow = w, () => new ImageSplitWindow(_jobManager, _settingsService));

    private void ShowRenameWindow() => ShowWindow(() => _renameWindow, w => _renameWindow = w, () => new RenameWindow(_jobManager, _settingsService));

    private void ShowUtf8Window() => ShowWindow(() => _utf8ConvertWindow, w => _utf8ConvertWindow = w, () => new Utf8ConvertWindow(_jobManager, _settingsService));

    private void ShowSnapshotWindow() => ShowWindow(() => _snapshotWindow, w => _snapshotWindow = w, () => new SnapshotWindow(_jobManager, _settingsService));

    private void ShowSshTunnelWindow() => ShowWindow(() => _sshTunnelWindow, w => _sshTunnelWindow = w, () => new SshTunnelWindow(_jobManager, _settingsService, _configService));

    private void ShowNgrokWindow() => ShowWindow(() => _ngrokWindow, w => _ngrokWindow = w, () => new NgrokWindow(_jobManager, _settingsService));

    private void ShowSearchTextWindow() => ShowWindow(() => _searchTextWindow, w => _searchTextWindow = w, () => new SearchTextWindow(_jobManager, _settingsService));

    private void ShowMigrationsWindow() => ShowWindow(() => _migrationsWindow, w => _migrationsWindow = w, () => new MigrationsWindow(_jobManager, _settingsService, _configService));

    private void ShowNotesWindow() => ShowWindow(() => _notesWindow, w => _notesWindow = w, () => new NotesWindow(_settingsService));

    public void ShowJobCenter() => ShowWindow(() => _jobCenterWindow, w => _jobCenterWindow = w, () => new JobCenterWindow(_jobManager));

    private void OnHarvestClick(object sender, RoutedEventArgs e)
    {
        ShowWindow(() => _harvestWindow, w => _harvestWindow = w, () => new HarvestWindow(_jobManager, _settingsService));
    }

    private void ShowLogsWindow() => ShowWindow(() => _logsWindow, w => _logsWindow = w, () => new LogsWindow(_settingsService));

    private void OnJobCompleted(string message, bool success)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            try 
            {
               // Atualiza ToolTip
               _taskbarIcon.ToolTipText = $"DevTools: {message}";
               
               if (!success)
               {
                   UiMessageService.ShowError(message, "Erro DevTools");
               }
               else
               {
                   // Feedback visual sutil (mudança de tooltip já ocorre)
               }
            }
            catch (Exception ex)
            {
                AppLogger.Error("Error updating UI in OnJobCompleted", ex);
            }
        });
    }

    public void Dispose()
    {
        _taskbarIcon?.Dispose();
    }
}
