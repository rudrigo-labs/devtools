using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using DevTools.Core.Configuration;
using DevTools.Presentation.Wpf.ToolRouting;
using DevTools.Presentation.Wpf.Utilities;
using DevTools.Presentation.Wpf.Views;
using DevTools.SSHTunnel.Engine;
using DevTools.SSHTunnel.Providers;
using H.NotifyIcon;
using WpfApplication = System.Windows.Application;

namespace DevTools.Presentation.Wpf.Services;

public class TrayService : IDisposable
{
    private readonly JobManager _jobManager;
    private readonly SettingsService _settingsService;
    private readonly ConfigService _configService;
    private readonly ToolConfigurationManager _toolConfigurationManager;
    private readonly GoogleDriveService _googleDriveService;
    private readonly TunnelService _sharedTunnelService;

    private readonly ToolServiceProvider _toolServices;
    private readonly ToolRegistry _toolRegistry = new();
    private readonly DetachedWindowLaunchStrategy _detachedWindowStrategy = new();
    private readonly ToolRouter _toolRouter;

    private TaskbarIcon? _taskbarIcon;
    private MainWindow? _mainWindow;

    public event Action<EmbeddedToolRequest>? EmbeddedToolRequested;

    public bool HasOpenToolWindow => _detachedWindowStrategy.HasOpenWindow;
    public bool HasActiveTunnel => _sharedTunnelService.IsOn;
    public bool IsInitialized => _taskbarIcon != null;

    public TrayService(JobManager jobManager, SettingsService settingsService, ConfigService configService, ToolConfigurationManager toolConfigurationManager, GoogleDriveService googleDriveService)
    {
        _jobManager = jobManager;
        _settingsService = settingsService;
        _configService = configService;
        _toolConfigurationManager = toolConfigurationManager;
        _googleDriveService = googleDriveService;
        _sharedTunnelService = new TunnelService(new SystemProcessRunner());

        _toolServices = new ToolServiceProvider()
            .Add(_jobManager)
            .Add(_settingsService)
            .Add(_configService)
            .Add(_toolConfigurationManager)
            .Add(_googleDriveService)
            .Add(_sharedTunnelService);

        _toolRouter = new ToolRouter(
            _toolRegistry,
            new IToolLaunchStrategy[]
            {
                _detachedWindowStrategy,
                new EmbeddedTabLaunchStrategy(),
                new BackgroundOnlyLaunchStrategy()
            });

        RegisterToolDefinitions();
        _jobManager.OnJobCompleted += OnJobCompleted;
    }

    public void SetMainWindow(MainWindow mainWindow)
    {
        _mainWindow = mainWindow;
        _toolServices.Add(mainWindow);
    }

    public void OpenTool(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return;
        }

        if (string.Equals(tag, "SSHTunnel", StringComparison.OrdinalIgnoreCase))
        {
            EnsureInitialized();
        }

        if (string.Equals(tag, "HIDE_CURRENT", StringComparison.OrdinalIgnoreCase))
        {
            _detachedWindowStrategy.CloseCurrentWindow();
            return;
        }

        if (_toolRouter.TryOpen(tag, BuildLaunchContext()))
        {
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
            {
                _mainWindow.WindowState = WindowState.Normal;
            }
        }
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

            var confirm = UiMessageService.Confirm(message, "Confirmar Saida");
            if (!confirm)
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
        WpfApplication.Current.Shutdown();
    }

    public void EnsureInitialized()
    {
        if (_taskbarIcon != null)
        {
            return;
        }

        Initialize();
    }

    public void Initialize()
    {
        if (_taskbarIcon != null)
        {
            return;
        }

        _taskbarIcon = new TaskbarIcon
        {
            ToolTipText = "DevTools Tray",
            ContextMenu = CreateContextMenu(),
            DoubleClickCommand = new DelegateCommand(ShowDashboard)
        };

        if (!TrySetIconSource(_taskbarIcon))
        {
            _taskbarIcon.Icon = SystemIcons.Application;
        }

        _taskbarIcon.ForceCreate();
    }

    public void Dispose()
    {
        _taskbarIcon?.Dispose();
        _taskbarIcon = null;
        _sharedTunnelService?.Dispose();
    }

    private ToolLaunchContext BuildLaunchContext()
    {
        return new ToolLaunchContext
        {
            Services = _toolServices,
            GetMainWindow = () => _mainWindow,
            RequestEmbeddedTool = ShowEmbeddedTool,
            LogInfo = AppLogger.Info,
            LogError = AppLogger.Error
        };
    }

    private void RegisterToolDefinitions()
    {
        RegisterTool(new ToolDescriptor
        {
            Id = "Dashboard",
            Title = "Dashboard",
            LaunchMode = ToolLaunchMode.EmbeddedTab,
            EmbeddedTarget = EmbeddedToolTarget.ToolsHome,
            Category = ToolCategory.Navigation,
            Order = 0,
            IconKey = "Icon.Dashboard"
        });

        RegisterTool(new ToolDescriptor
        {
            Id = "Jobs",
            Title = "Centro de Execucoes",
            LaunchMode = ToolLaunchMode.EmbeddedTab,
            EmbeddedTarget = EmbeddedToolTarget.JobsTab,
            Category = ToolCategory.Navigation,
            Order = 10,
            IconKey = "Icon.Jobs"
        });

        RegisterTool(new ToolDescriptor
        {
            Id = "Logs",
            Title = "Logs do Sistema",
            Subtitle = "Consulta e manutencao de logs no shell principal.",
            LaunchMode = ToolLaunchMode.EmbeddedTab,
            EmbeddedTarget = EmbeddedToolTarget.EmbeddedHost,
            Factory = _ => new EmbeddedLogsView(),
            Category = ToolCategory.Diagnostics,
            Order = 20,
            IconKey = "Icon.Logs"
        });

        RegisterTool(new ToolDescriptor
        {
            Id = "Notes",
            Title = "Notas Rapidas",
            LaunchMode = ToolLaunchMode.DetachedWindow,
            Factory = services => new NotesWindow(
                services.GetRequiredService<SettingsService>(),
                services.GetRequiredService<GoogleDriveService>(),
                services.GetRequiredService<ConfigService>()),
            Category = ToolCategory.Productivity,
            Order = 30,
            IconKey = "Icon.Notes"
        });

        RegisterTool(new ToolDescriptor
        {
            Id = "Organizer",
            Title = "Organizador de Arquivos",
            LaunchMode = ToolLaunchMode.DetachedWindow,
            Factory = services => new OrganizerWindow(
                services.GetRequiredService<JobManager>(),
                services.GetRequiredService<SettingsService>()),
            Category = ToolCategory.Productivity,
            Order = 40,
            IconKey = "Icon.Organizer"
        });

        RegisterTool(new ToolDescriptor
        {
            Id = "Harvest",
            Title = "Harvest",
            LaunchMode = ToolLaunchMode.DetachedWindow,
            Factory = services => new HarvestWindow(
                services.GetRequiredService<JobManager>(),
                services.GetRequiredService<SettingsService>(),
                services.GetRequiredService<ToolConfigurationManager>()),
            Category = ToolCategory.Productivity,
            Order = 50,
            IconKey = "Icon.Harvest"
        });

        RegisterTool(new ToolDescriptor
        {
            Id = "SearchText",
            Title = "Busca de Texto",
            LaunchMode = ToolLaunchMode.DetachedWindow,
            Factory = services => new SearchTextWindow(
                services.GetRequiredService<JobManager>(),
                services.GetRequiredService<SettingsService>(),
                services.GetRequiredService<ToolConfigurationManager>()),
            Category = ToolCategory.Productivity,
            Order = 60,
            IconKey = "Icon.Search"
        });

        RegisterTool(new ToolDescriptor
        {
            Id = "Migrations",
            Title = "Migrations",
            LaunchMode = ToolLaunchMode.DetachedWindow,
            Factory = services => new MigrationsWindow(
                services.GetRequiredService<JobManager>(),
                services.GetRequiredService<SettingsService>(),
                services.GetRequiredService<ConfigService>(),
                services.GetRequiredService<ToolConfigurationManager>()),
            Category = ToolCategory.Infrastructure,
            Order = 70,
            IconKey = "Icon.Migrations"
        });

        RegisterTool(new ToolDescriptor
        {
            Id = "ImageSplitter",
            Title = "Dividir Imagens",
            LaunchMode = ToolLaunchMode.DetachedWindow,
            Factory = services => new ImageSplitWindow(
                services.GetRequiredService<JobManager>(),
                services.GetRequiredService<SettingsService>()),
            Category = ToolCategory.Productivity,
            Order = 80,
            IconKey = "Icon.ImageSplitter"
        });

        RegisterTool(new ToolDescriptor
        {
            Id = "Rename",
            Title = "Renomear Projeto .NET",
            LaunchMode = ToolLaunchMode.DetachedWindow,
            Factory = services => new RenameWindow(
                services.GetRequiredService<JobManager>(),
                services.GetRequiredService<SettingsService>(),
                services.GetRequiredService<ToolConfigurationManager>()),
            Category = ToolCategory.Productivity,
            Order = 90,
            IconKey = "Icon.Rename"
        });

        RegisterTool(new ToolDescriptor
        {
            Id = "Utf8Convert",
            Title = "Converter para UTF-8",
            LaunchMode = ToolLaunchMode.DetachedWindow,
            Factory = services => new Utf8ConvertWindow(
                services.GetRequiredService<JobManager>(),
                services.GetRequiredService<SettingsService>()),
            Category = ToolCategory.Infrastructure,
            Order = 100,
            IconKey = "Icon.Utf8Convert"
        });

        RegisterTool(new ToolDescriptor
        {
            Id = "Snapshot",
            Title = "Snapshot de Projeto",
            LaunchMode = ToolLaunchMode.DetachedWindow,
            Factory = services => new SnapshotWindow(
                services.GetRequiredService<JobManager>(),
                services.GetRequiredService<SettingsService>(),
                services.GetRequiredService<ToolConfigurationManager>()),
            Category = ToolCategory.Infrastructure,
            Order = 110,
            IconKey = "Icon.Snapshot"
        });

        RegisterTool(new ToolDescriptor
        {
            Id = "SSHTunnel",
            Title = "Tunel SSH",
            LaunchMode = ToolLaunchMode.DetachedWindow,
            Factory = services => new SshTunnelWindow(
                services.GetRequiredService<JobManager>(),
                services.GetRequiredService<SettingsService>(),
                services.GetRequiredService<ConfigService>(),
                services.GetRequiredService<ToolConfigurationManager>(),
                services.GetRequiredService<TunnelService>()),
            Category = ToolCategory.Infrastructure,
            Order = 120,
            IconKey = "Icon.SSHTunnel"
        });

        RegisterTool(new ToolDescriptor
        {
            Id = "Ngrok",
            Title = "Ngrok",
            LaunchMode = ToolLaunchMode.DetachedWindow,
            Factory = services => new NgrokWindow(
                services.GetRequiredService<SettingsService>()),
            Category = ToolCategory.Infrastructure,
            Order = 130,
            IconKey = "Icon.Ngrok"
        });
    }

    private void RegisterTool(ToolDescriptor descriptor)
    {
        _toolRegistry.Register(descriptor);
    }

    private void ShowEmbeddedTool(EmbeddedToolRequest request)
    {
        WpfApplication.Current.Dispatcher.Invoke(() =>
        {
            if (_mainWindow != null)
            {
                _mainWindow.Show();
                _mainWindow.Activate();
                if (_mainWindow.WindowState == WindowState.Minimized)
                {
                    _mainWindow.WindowState = WindowState.Normal;
                }
            }

            EmbeddedToolRequested?.Invoke(request);
        });
    }

    private bool TrySetIconSource(TaskbarIcon taskbarIcon)
    {
        try
        {
            var uriSimple = new Uri("pack://application:,,,/Assets/app.ico");
            var streamInfo = WpfApplication.GetResourceStream(uriSimple);
            if (streamInfo != null)
            {
                taskbarIcon.Icon = new Icon(streamInfo.Stream);
                return true;
            }

            var uriQualified = new Uri("pack://application:,,,/DevTools.Presentation.Wpf;component/Assets/app.ico");
            var streamInfo2 = WpfApplication.GetResourceStream(uriQualified);
            if (streamInfo2 != null)
            {
                taskbarIcon.Icon = new Icon(streamInfo2.Stream);
                return true;
            }

            var candidatePath = Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
            if (File.Exists(candidatePath))
            {
                taskbarIcon.Icon = new Icon(candidatePath);
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
        if (WpfApplication.Current.FindResource("TrayContextMenu") is System.Windows.Controls.ContextMenu menu)
        {
            foreach (var item in menu.Items)
            {
                if (item is System.Windows.Controls.MenuItem menuItem && menuItem.Tag is string tag)
                {
                    if (string.Equals(tag, "Exit", StringComparison.OrdinalIgnoreCase))
                    {
                        menuItem.Click += async (_, _) => await RequestExitAsync();
                    }
                    else if (_toolRegistry.TryGet(tag, out var tool) && tool.IsEnabled)
                    {
                        menuItem.Click += (_, _) => OpenTool(tag);
                    }
                }
            }

            return menu;
        }

        var fallbackMenu = new System.Windows.Controls.ContextMenu();
        var jobs = new System.Windows.Controls.MenuItem { Header = "Jobs (Fallback)" };
        jobs.Click += (_, _) => OpenTool("Jobs");
        fallbackMenu.Items.Add(jobs);

        var itemExit = new System.Windows.Controls.MenuItem { Header = "Sair (Fallback)" };
        itemExit.Click += async (_, _) => await RequestExitAsync();
        fallbackMenu.Items.Add(itemExit);

        return fallbackMenu;
    }

    private void OnJobCompleted(string message, bool success)
    {
        WpfApplication.Current.Dispatcher.Invoke(() =>
        {
            try
            {
                if (_taskbarIcon != null)
                {
                    _taskbarIcon.ToolTipText = $"DevTools: {message}";
                }

                if (!success)
                {
                    UiMessageService.ShowError(message, "Erro DevTools");
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("Error updating UI in OnJobCompleted", ex);
            }
        });
    }
}

