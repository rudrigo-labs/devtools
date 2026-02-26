using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using H.NotifyIcon;
using System.Drawing; // SystemIcons fallback
using System.IO;
using DevTools.Presentation.Wpf.Utilities;
using DevTools.Core.Configuration;
using DevTools.Presentation.Wpf.Views;

namespace DevTools.Presentation.Wpf.Services;

public class TrayService : IDisposable
{
    private readonly JobManager _jobManager;
    private readonly SettingsService _settingsService;
    private readonly ConfigService _configService;
    private readonly ProfileManager _profileManager;

    private TaskbarIcon _taskbarIcon = null!;
    private Window? _dashboardWindow;
    private SshTunnelWindow? _sshTunnelWindow;
    private NotesWindow? _notesWindow;
    private OrganizerWindow? _organizerWindow;
    private RenameWindow? _renameWindow;
    private SearchTextWindow? _searchTextWindow;
    private ImageSplitWindow? _imageSplitWindow;
    private MigrationsWindow? _migrationsWindow;
    private HarvestWindow? _harvestWindow;
    private Utf8Window? _utf8Window;
    private SnapshotWindow? _snapshotWindow;
    private NgrokWindow? _ngrokWindow;
    private LogsWindow? _logsWindow;

    public void SetDashboardWindow(Window dashboardWindow)
    {
        _dashboardWindow = dashboardWindow;
    }

    public void OpenTool(string tag)
    {
        switch (tag)
        {
            case "Notes": ShowNotesWindow(); break;
            case "Organizer": ShowOrganizerWindow(); break;
            case "Harvest": ShowHarvestWindow(); break;
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

    public void CloseAllTools()
    {
        _notesWindow?.Hide(); 
        
        CloseWindow(ref _organizerWindow);
        CloseWindow(ref _harvestWindow);
        CloseWindow(ref _searchTextWindow);
        CloseWindow(ref _migrationsWindow);
        CloseWindow(ref _imageSplitWindow);
        CloseWindow(ref _renameWindow);
        CloseWindow(ref _utf8Window);
        CloseWindow(ref _snapshotWindow);
        CloseWindow(ref _sshTunnelWindow);
        CloseWindow(ref _ngrokWindow);
        CloseWindow(ref _logsWindow);
        
        _dashboardWindow?.Hide();
    }

    private void CloseWindow<T>(ref T? window) where T : Window
    {
        if (window != null)
        {
            try { window.Close(); } catch { }
            window = null;
        }
    }

    public void ShowDashboard()
    {
        CloseAllTools();
        if (_dashboardWindow != null)
        {
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
                            menuItem.Click += (s, e) => ShowHarvestWindow();
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

        var fallbackMenu = new System.Windows.Controls.ContextMenu();

        var itemExit = new System.Windows.Controls.MenuItem { Header = "Sair" };
        itemExit.Click += (s, e) => System.Windows.Application.Current.Shutdown();
        fallbackMenu.Items.Add(itemExit);

        return fallbackMenu;
    }

    private void PositionWindowBottomRight(Window window)
    {
        if (window == null) return;
        
        var desktopWorkingArea = SystemParameters.WorkArea;
        window.Left = desktopWorkingArea.Right - window.Width - 20;
        window.Top = desktopWorkingArea.Bottom - window.Height - 20;
    }

    private void ShowOrganizerWindow()
    {
        CloseAllTools();
        if (_organizerWindow == null || !_organizerWindow.IsLoaded)
        {
            _organizerWindow = new OrganizerWindow(_jobManager, _settingsService);
        }

        PositionWindowBottomRight(_organizerWindow);
        _organizerWindow.Show();
        if (_organizerWindow.WindowState == WindowState.Minimized)
            _organizerWindow.WindowState = WindowState.Normal;
        _organizerWindow.Activate();
    }

    private void ShowImageSplitWindow()
    {
        CloseAllTools();
        if (_imageSplitWindow == null || !_imageSplitWindow.IsLoaded)
        {
            _imageSplitWindow = new ImageSplitWindow(_jobManager, _settingsService);
        }

        PositionWindowBottomRight(_imageSplitWindow);
        _imageSplitWindow.Show();
        if (_imageSplitWindow.WindowState == WindowState.Minimized)
            _imageSplitWindow.WindowState = WindowState.Normal;
        _imageSplitWindow.Activate();
    }

    private void ShowRenameWindow()
    {
        CloseAllTools();
        if (_renameWindow == null || !_renameWindow.IsLoaded)
        {
            _renameWindow = new RenameWindow(_jobManager, _settingsService);
        }

        PositionWindowBottomRight(_renameWindow);
        _renameWindow.Show();
        if (_renameWindow.WindowState == WindowState.Minimized)
            _renameWindow.WindowState = WindowState.Normal;
        _renameWindow.Activate();
    }

    private void ShowUtf8Window()
    {
        CloseAllTools();
        if (_utf8Window == null || !_utf8Window.IsLoaded)
        {
            _utf8Window = new Utf8Window(_jobManager, _settingsService);
        }

        PositionWindowBottomRight(_utf8Window);
        _utf8Window.Show();
        if (_utf8Window.WindowState == WindowState.Minimized)
            _utf8Window.WindowState = WindowState.Normal;
        _utf8Window.Activate();
    }

    private void ShowSnapshotWindow()
    {
        CloseAllTools();
        if (_snapshotWindow == null || !_snapshotWindow.IsLoaded)
        {
            _snapshotWindow = new SnapshotWindow(_settingsService);
        }

        PositionWindowBottomRight(_snapshotWindow);
        _snapshotWindow.Show();
        if (_snapshotWindow.WindowState == WindowState.Minimized)
            _snapshotWindow.WindowState = WindowState.Normal;
        _snapshotWindow.Activate();
    }

    private void ShowSshTunnelWindow()
    {
        CloseAllTools();
        if (_sshTunnelWindow == null || !_sshTunnelWindow.IsLoaded)
        {
            _sshTunnelWindow = new SshTunnelWindow(_settingsService);
        }

        PositionWindowBottomRight(_sshTunnelWindow);
        _sshTunnelWindow.Show();
        if (_sshTunnelWindow.WindowState == WindowState.Minimized)
            _sshTunnelWindow.WindowState = WindowState.Normal;
        _sshTunnelWindow.Activate();
    }

    private void ShowHarvestWindow()
    {
        CloseAllTools();
        if (_harvestWindow == null || !_harvestWindow.IsLoaded)
        {
            _harvestWindow = new HarvestWindow(_jobManager, _settingsService);
        }

        PositionWindowBottomRight(_harvestWindow);
        _harvestWindow.Show();
        if (_harvestWindow.WindowState == WindowState.Minimized)
            _harvestWindow.WindowState = WindowState.Normal;
        _harvestWindow.Activate();
    }

    private void ShowSearchTextWindow()
    {
        CloseAllTools();
        if (_searchTextWindow == null || !_searchTextWindow.IsLoaded)
        {
            _searchTextWindow = new SearchTextWindow(_jobManager, _settingsService);
        }

        PositionWindowBottomRight(_searchTextWindow);
        _searchTextWindow.Show();
        if (_searchTextWindow.WindowState == WindowState.Minimized)
            _searchTextWindow.WindowState = WindowState.Normal;
        _searchTextWindow.Activate();
    }

    private void ShowMigrationsWindow()
    {
        CloseAllTools();
        if (_migrationsWindow == null || !_migrationsWindow.IsLoaded)
        {
            _migrationsWindow = new MigrationsWindow(_jobManager, _settingsService, _configService);
        }

        PositionWindowBottomRight(_migrationsWindow);
        _migrationsWindow.Show();
        if (_migrationsWindow.WindowState == WindowState.Minimized)
            _migrationsWindow.WindowState = WindowState.Normal;
        _migrationsWindow.Activate();
    }

    private void ShowNgrokWindow()
    {
        CloseAllTools();
        if (_ngrokWindow == null || !_ngrokWindow.IsLoaded)
        {
            _ngrokWindow = new NgrokWindow(_settingsService);
        }

        PositionWindowBottomRight(_ngrokWindow);
        _ngrokWindow.Show();
        if (_ngrokWindow.WindowState == WindowState.Minimized)
            _ngrokWindow.WindowState = WindowState.Normal;
        _ngrokWindow.Activate();
    }

    private void ShowJobCenter()
    {
        CloseAllTools();
        if (_dashboardWindow != null)
        {
            _dashboardWindow.Show();
            
            if (_dashboardWindow.WindowState == WindowState.Minimized)
                _dashboardWindow.WindowState = WindowState.Normal;
            
            _dashboardWindow.Activate();
            
            if (_dashboardWindow is DashboardWindow dash)
            {
                dash.SelectJobsTab();
            }
        }
    }

    private void ShowNotesWindow()
    {
        CloseAllTools();
        if (_notesWindow == null || !_notesWindow.IsLoaded)
        {
            _notesWindow = new NotesWindow(_settingsService);
        }

        PositionWindowBottomRight(_notesWindow);
        _notesWindow.Show();
        if (_notesWindow.WindowState == WindowState.Minimized)
            _notesWindow.WindowState = WindowState.Normal;
        _notesWindow.Activate();
    }

    private void ShowLogsWindow()
    {
        CloseAllTools();
        if (_logsWindow == null || !_logsWindow.IsLoaded)
        {
            _logsWindow = new LogsWindow();
        }

        PositionWindowBottomRight(_logsWindow);
        _logsWindow.Show();
        if (_logsWindow.WindowState == WindowState.Minimized)
            _logsWindow.WindowState = WindowState.Normal;
        _logsWindow.Activate();
    }

    private static void ShowToolNotImplemented(string toolName)
    {
        DevToolsMessage.Info(
            $"A interface da ferramenta \"{toolName}\" ainda não foi reimplementada nesta branch.",
            "DevTools");
    }

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
                   DevToolsMessage.Error(message, "Erro DevTools");
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
