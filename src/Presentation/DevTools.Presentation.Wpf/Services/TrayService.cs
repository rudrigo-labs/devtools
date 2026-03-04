using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using H.NotifyIcon;
using System.Drawing; // SystemIcons fallback
using System.IO;
using DevTools.Presentation.Wpf.Views;
using DevTools.Presentation.Wpf.Utilities;
using DevTools.Core.Configuration;
using DevTools.SSHTunnel.Engine;
using DevTools.SSHTunnel.Providers;

namespace DevTools.Presentation.Wpf.Services;

public class TrayService : IDisposable
{
    private enum LaunchMode
    {
        DetachedWindow,
        EmbeddedTab,
        BackgroundOnly
    }

    private sealed record ToolLaunchDefinition(string Tag, LaunchMode Mode, Action OpenAction);

    private readonly JobManager _jobManager;
    private readonly SettingsService _settingsService;
    private readonly ConfigService _configService;
    private readonly ProfileManager _profileManager;
    private readonly GoogleDriveService _googleDriveService;
    private readonly TunnelService _sharedTunnelService;
    private readonly Dictionary<string, ToolLaunchDefinition> _toolRegistry = new(StringComparer.OrdinalIgnoreCase);

    private TaskbarIcon _taskbarIcon = null!;
    private Window? _jobCenterWindow;
    private NotesWindow? _notesWindow;
    private MainWindow? _mainWindow;

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
    public event Action<string>? EmbeddedToolRequested;

    public void SetMainWindow(MainWindow mainWindow)
    {
        _mainWindow = mainWindow;
    }

    public bool HasOpenToolWindow => _currentToolWindow != null && _currentToolWindow.IsVisible;
    public bool HasActiveTunnel => _sharedTunnelService.IsOn;

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

            // Set owner and child behavior
            if (_mainWindow != null && window != _mainWindow)
            {
                window.Owner = _mainWindow;
                window.ShowInTaskbar = false;
                window.WindowStartupLocation = WindowStartupLocation.Manual;
            }

            window.Closed += (_, __) => 
            {
                setWindow(null);
                if (_currentToolWindow == window)
                    _currentToolWindow = null;

                // Restore MainWindow when tool is closed
                if (_mainWindow != null)
                {
                    _mainWindow.IsEnabled = true; // Ensure it's enabled
                    if (_mainWindow.IsVisible)
                    {
                        _mainWindow.Activate();
                    }
                }
            };

            // Enforce Bottom-Right Positioning for all tool windows
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
        if (string.IsNullOrWhiteSpace(tag))
        {
            return;
        }

        if (string.Equals(tag, "HIDE_CURRENT", StringComparison.OrdinalIgnoreCase))
        {
            if (_currentToolWindow != null && _currentToolWindow.IsVisible)
            {
                _currentToolWindow.Close();
            }

            return;
        }

        if (_toolRegistry.TryGetValue(tag, out var tool))
        {
            if (tool.Mode == LaunchMode.EmbeddedTab)
            {
                if (_currentToolWindow != null && _currentToolWindow.IsVisible)
                {
                    _currentToolWindow.Close();
                    _currentToolWindow = null;
                }

                ShowEmbeddedTool(tool.Tag);
            }
            else
            {
                tool.OpenAction();
            }

            return;
        }

        AppLogger.Info($"Unknown tool tag received: {tag}");
    }

    public void ShowDashboard()
    {
        if (_mainWindow != null)
        {
            _mainWindow.ResetToHome();
            _mainWindow.Show();
            _mainWindow.Activate();
            if (_mainWindow.WindowState == WindowState.Minimized)
                _mainWindow.WindowState = WindowState.Normal;
        }
    }

    public TrayService(JobManager jobManager, SettingsService settingsService, ConfigService configService, ProfileManager profileManager, GoogleDriveService googleDriveService)
    {
        _jobManager = jobManager;
        _settingsService = settingsService;
        _configService = configService;
        _profileManager = profileManager;
        _googleDriveService = googleDriveService;
        _sharedTunnelService = new TunnelService(new SystemProcessRunner());
        RegisterToolDefinitions();
        _jobManager.OnJobCompleted += OnJobCompleted;
    }

    public async Task RequestExitAsync(bool skipConfirmation = false)
    {
        var runningJobs = _jobManager.RunningJobsCount;
        var hasActiveTunnel = _sharedTunnelService.IsOn;

        if (!skipConfirmation && (runningJobs > 0 || hasActiveTunnel))
        {
            var details = new List<string>();
            if (runningJobs > 0)
            {
                details.Add($"- Jobs em execucao: {runningJobs}");
            }

            if (hasActiveTunnel)
            {
                details.Add("- Tunel SSH ativo");
            }

            var message = "Existem operacoes ativas:\n"
                + string.Join("\n", details)
                + "\n\nDeseja encerrar operacoes e sair?";

            System.Windows.MessageBoxResult confirm = System.Windows.MessageBox.Show(
                message,
                "Confirmar Saida",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (confirm != System.Windows.MessageBoxResult.Yes)
            {
                return;
            }
        }

        _jobManager.CancelAllRunningJobs();

        if (_sharedTunnelService.IsOn)
        {
            try
            {
                await _sharedTunnelService.StopAsync(TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                AppLogger.Error("Falha ao encerrar tunel SSH durante saida.", ex);
            }
        }

        _mainWindow?.AllowCloseForShutdown();
        System.Windows.Application.Current.Shutdown();
    }

    private void RegisterToolDefinitions()
    {
        RegisterTool("Dashboard", LaunchMode.EmbeddedTab, ShowDashboard);

        RegisterTool("Notes", LaunchMode.DetachedWindow, ShowNotesWindow);
        RegisterTool("Organizer", LaunchMode.DetachedWindow, ShowOrganizerWindow);
        RegisterTool("Harvest", LaunchMode.DetachedWindow, ShowHarvestWindow);
        RegisterTool("SearchText", LaunchMode.DetachedWindow, ShowSearchTextWindow);
        RegisterTool("Migrations", LaunchMode.DetachedWindow, ShowMigrationsWindow);
        RegisterTool("ImageSplitter", LaunchMode.DetachedWindow, ShowImageSplitWindow);
        RegisterTool("Rename", LaunchMode.DetachedWindow, ShowRenameWindow);
        RegisterTool("Utf8Convert", LaunchMode.DetachedWindow, ShowUtf8Window);
        RegisterTool("Snapshot", LaunchMode.DetachedWindow, ShowSnapshotWindow);
        RegisterTool("SSHTunnel", LaunchMode.DetachedWindow, ShowSshTunnelWindow);
        RegisterTool("Ngrok", LaunchMode.DetachedWindow, ShowNgrokWindow);
        RegisterTool("Jobs", LaunchMode.EmbeddedTab, ShowJobCenter);
        RegisterTool("Logs", LaunchMode.EmbeddedTab, ShowLogsWindow);
    }

    private void RegisterTool(string tag, LaunchMode mode, Action openAction)
    {
        _toolRegistry[tag] = new ToolLaunchDefinition(tag, mode, openAction);
    }

    private void ShowEmbeddedTool(string tag)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            if (_mainWindow != null)
            {
                _mainWindow.Show();
                _mainWindow.Activate();
                if (_mainWindow.WindowState == WindowState.Minimized)
                    _mainWindow.WindowState = WindowState.Normal;
            }

            EmbeddedToolRequested?.Invoke(tag);
        });
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
                    if (string.Equals(tag, "Exit", StringComparison.OrdinalIgnoreCase))
                    {
                        menuItem.Click += async (s, e) => await RequestExitAsync();
                    }
                    else if (_toolRegistry.ContainsKey(tag))
                    {
                        menuItem.Click += (s, e) => OpenTool(tag);
                    }
                }
            }

            return menu;
        }

        // Fallback básico caso o recurso não seja encontrado
        var fallbackMenu = new System.Windows.Controls.ContextMenu();

        var jobs = new System.Windows.Controls.MenuItem { Header = "Jobs (Fallback)" };
        jobs.Click += (s, e) => OpenTool("Jobs");
        fallbackMenu.Items.Add(jobs);

        var itemExit = new System.Windows.Controls.MenuItem { Header = "Sair (Fallback)" };
        itemExit.Click += async (s, e) => await RequestExitAsync();
        fallbackMenu.Items.Add(itemExit);

        return fallbackMenu;
    }

    private void ShowOrganizerWindow() => ShowWindow(() => _organizerWindow, w => _organizerWindow = w, () => new OrganizerWindow(_jobManager, _settingsService));

    private void ShowImageSplitWindow() => ShowWindow(() => _imageSplitWindow, w => _imageSplitWindow = w, () => new ImageSplitWindow(_jobManager, _settingsService));

    private void ShowRenameWindow() => ShowWindow(() => _renameWindow, w => _renameWindow = w, () => new RenameWindow(_jobManager, _settingsService, _profileManager));

    private void ShowUtf8Window() => ShowWindow(() => _utf8ConvertWindow, w => _utf8ConvertWindow = w, () => new Utf8ConvertWindow(_jobManager, _settingsService));

    private void ShowSnapshotWindow() => ShowWindow(() => _snapshotWindow, w => _snapshotWindow = w, () => new SnapshotWindow(_jobManager, _settingsService, _profileManager));

    private void ShowSshTunnelWindow() => ShowWindow(() => _sshTunnelWindow, w => _sshTunnelWindow = w, () => new SshTunnelWindow(_jobManager, _settingsService, _configService, _profileManager, _sharedTunnelService));

    private void ShowNgrokWindow() => ShowWindow(() => _ngrokWindow, w => _ngrokWindow = w, () => new NgrokWindow(_jobManager, _settingsService));

    private void ShowSearchTextWindow() => ShowWindow(() => _searchTextWindow, w => _searchTextWindow = w, () => new SearchTextWindow(_jobManager, _settingsService, _profileManager));

    private void ShowMigrationsWindow() => ShowWindow(() => _migrationsWindow, w => _migrationsWindow = w, () => new MigrationsWindow(_jobManager, _settingsService, _configService, _profileManager));

    private void ShowNotesWindow() => ShowWindow(() => _notesWindow, w => _notesWindow = w, () => new NotesWindow(_settingsService, _googleDriveService, _configService));

    public void ShowJobCenter() => ShowWindow(() => _jobCenterWindow, w => _jobCenterWindow = w, () => new JobCenterWindow(_jobManager));

    private void ShowHarvestWindow() => ShowWindow(() => _harvestWindow, w => _harvestWindow = w, () => new HarvestWindow(_jobManager, _settingsService, _profileManager));

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
        _sharedTunnelService?.Dispose();
    }
}
