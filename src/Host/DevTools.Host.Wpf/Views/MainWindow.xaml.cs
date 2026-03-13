using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace DevTools.Host.Wpf.Views;

public partial class MainWindow : Window
{
    private const uint MonitorDefaultToNearest = 0x00000002;
    private const string FerramentasTag = "Ferramentas";
    private const string ConfiguracoesTag = "Configuracoes";

    private enum WorkspaceIntent { Default, Configuration, Execution }

    private readonly Dictionary<string, Func<System.Windows.Controls.UserControl>> _toolRegistry;
    private string _activeToolTag = string.Empty;
    private WorkspaceIntent _activeIntent = WorkspaceIntent.Default;
    private string _activeSidebarTag = FerramentasTag;
    private bool _isWorkAreaMaximized;
    private Rect _restoreBounds;

    public MainWindow(
        HomeLauncherView homeLauncherView,
        ConfigurationLauncherView configurationLauncherView,
        SnapshotWorkspaceView snapshotWorkspaceView,
        RenameWorkspaceView renameWorkspaceView,
        HarvestWorkspaceView harvestWorkspaceView,
        ImageSplitWorkspaceView imageSplitWorkspaceView,
        SearchTextWorkspaceView searchTextWorkspaceView,
        OrganizerWorkspaceView organizerWorkspaceView,
        Utf8ConvertWorkspaceView utf8ConvertWorkspaceView,
        MigrationsWorkspaceView migrationsWorkspaceView,
        SshTunnelWorkspaceView sshTunnelWorkspaceView,
        NgrokWorkspaceView ngrokWorkspaceView,
        NotesWorkspaceView notesWorkspaceView)
    {
        InitializeComponent();

        _toolRegistry = new Dictionary<string, Func<System.Windows.Controls.UserControl>>(StringComparer.OrdinalIgnoreCase)
        {
            [FerramentasTag] = () => homeLauncherView,
            [ConfiguracoesTag] = () => configurationLauncherView,
            ["Snapshot"] = () => snapshotWorkspaceView,
            ["Rename"] = () => renameWorkspaceView,
            ["Harvest"] = () => harvestWorkspaceView,
            ["ImageSplit"] = () => imageSplitWorkspaceView,
            ["SearchText"] = () => searchTextWorkspaceView,
            ["Organizer"] = () => organizerWorkspaceView,
            ["Utf8Convert"] = () => utf8ConvertWorkspaceView,
            ["Migrations"] = () => migrationsWorkspaceView,
            ["SshTunnel"] = () => sshTunnelWorkspaceView,
            ["Ngrok"] = () => ngrokWorkspaceView,
            ["Notes"] = () => notesWorkspaceView
        };

        homeLauncherView.OpenToolRequested += toolTag =>
            ActivateTool(toolTag, WorkspaceIntent.Execution, FerramentasTag);

        configurationLauncherView.OpenToolRequested += toolTag =>
            ActivateTool(toolTag, WorkspaceIntent.Configuration, ConfiguracoesTag);

        Loaded += (_, _) =>
        {
            _restoreBounds = new Rect(Left, Top, Width, Height);
            MaximizeToWorkArea();
            ActivateTool(FerramentasTag, WorkspaceIntent.Default, FerramentasTag);
        };
    }

    private void NavButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button btn || btn.Tag is not string tag)
            return;

        if (string.Equals(tag, FerramentasTag, StringComparison.OrdinalIgnoreCase))
        {
            ActivateTool(FerramentasTag, WorkspaceIntent.Default, FerramentasTag);
            return;
        }

        if (string.Equals(tag, ConfiguracoesTag, StringComparison.OrdinalIgnoreCase))
        {
            ActivateTool(ConfiguracoesTag, WorkspaceIntent.Default, ConfiguracoesTag);
        }
    }

    private void ActivateTool(string tag, WorkspaceIntent intent = WorkspaceIntent.Default, string? sidebarTag = null)
    {
        if (string.Equals(_activeToolTag, tag, StringComparison.OrdinalIgnoreCase) && _activeIntent == intent)
            return;

        if (!_toolRegistry.TryGetValue(tag, out var factory))
            return;

        var workspace = factory();
        ApplyWorkspaceIntent(workspace, intent);

        _activeToolTag = tag;
        _activeIntent = intent;
        if (!string.IsNullOrWhiteSpace(sidebarTag))
            _activeSidebarTag = sidebarTag;

        WorkspaceHost.Content = workspace;
        UpdateHeaderAndStatus(tag, intent);
        UpdateNavStyles();
    }

    private static void ApplyWorkspaceIntent(System.Windows.Controls.UserControl workspace, WorkspaceIntent intent)
    {
        switch (workspace)
        {
            case SnapshotWorkspaceView snapshot:
                if (intent == WorkspaceIntent.Configuration) snapshot.ActivateConfigurationMode();
                else if (intent == WorkspaceIntent.Execution) snapshot.ActivateExecutionMode();
                break;
            case MigrationsWorkspaceView migrations:
                if (intent == WorkspaceIntent.Configuration) migrations.ActivateConfigurationMode();
                else if (intent == WorkspaceIntent.Execution) migrations.ActivateExecutionMode();
                break;
            case SshTunnelWorkspaceView sshTunnel:
                if (intent == WorkspaceIntent.Configuration) sshTunnel.ActivateConfigurationMode();
                else if (intent == WorkspaceIntent.Execution) sshTunnel.ActivateExecutionMode();
                break;
            case NgrokWorkspaceView ngrok:
                if (intent == WorkspaceIntent.Configuration) ngrok.ActivateConfigurationMode();
                else if (intent == WorkspaceIntent.Execution) ngrok.ActivateExecutionMode();
                break;
            case NotesWorkspaceView notes:
                if (intent == WorkspaceIntent.Configuration) notes.ActivateConfigurationMode();
                else if (intent == WorkspaceIntent.Execution) notes.ActivateExecutionMode();
                break;
        }
    }

    private void UpdateHeaderAndStatus(string tag, WorkspaceIntent intent)
    {
        if (string.Equals(tag, FerramentasTag, StringComparison.OrdinalIgnoreCase))
        {
            ActiveToolLabel.Text = "Ferramentas";
            MainStatusText.Text = "Selecione uma ferramenta para executar.";
            return;
        }

        if (string.Equals(tag, ConfiguracoesTag, StringComparison.OrdinalIgnoreCase))
        {
            ActiveToolLabel.Text = "Configuracoes";
            MainStatusText.Text = "Selecione uma ferramenta para editar configuracoes.";
            return;
        }

        var suffix = intent switch
        {
            WorkspaceIntent.Configuration => "Configuration",
            WorkspaceIntent.Execution => "Execution",
            _ => string.Empty
        };

        ActiveToolLabel.Text = string.IsNullOrWhiteSpace(suffix) ? tag : $"{tag} {suffix}";
        MainStatusText.Text = $"{ActiveToolLabel.Text} ativo.";
    }

    private void UpdateNavStyles()
    {
        var activeStyle = TryFindResource("SidebarNavButtonActiveStyle") as Style;
        var normalStyle = TryFindResource("SidebarNavButtonStyle") as Style;

        var isFerramentas = string.Equals(_activeSidebarTag, FerramentasTag, StringComparison.OrdinalIgnoreCase);
        var isConfiguracoes = string.Equals(_activeSidebarTag, ConfiguracoesTag, StringComparison.OrdinalIgnoreCase);

        NavFerramentas.Style = isFerramentas ? activeStyle : normalStyle;
        NavConfiguracoes.Style = isConfiguracoes ? activeStyle : normalStyle;
    }

    private void WindowDragArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left) return;
        if (_isWorkAreaMaximized)
        {
            RestoreFromWorkArea();
            Left = e.GetPosition(this).X - (Width / 2);
            Top = 0;
        }

        try { DragMove(); } catch { }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isWorkAreaMaximized)
        {
            RestoreFromWorkArea();
            return;
        }

        _restoreBounds = new Rect(Left, Top, Width, Height);
        MaximizeToWorkArea();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
        => Close();

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        var result = DevTools.Host.Wpf.Components.DevToolsMessageBox.Confirm(
            this,
            "Deseja realmente encerrar o DevTools?",
            "Encerrar");

        if (result == DevTools.Host.Wpf.Components.DevToolsMessageBoxResult.Yes)
            Close();
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        if (RootBorder is null) return;

        if (WindowState == WindowState.Maximized)
        {
            _restoreBounds = new Rect(RestoreBounds.Left, RestoreBounds.Top, RestoreBounds.Width, RestoreBounds.Height);
            MaximizeToWorkArea();
        }
    }

    private void MaximizeToWorkArea()
    {
        var workArea = ResolveCurrentWorkArea();
        WindowState = WindowState.Normal;
        Left = workArea.Left;
        Top = workArea.Top;
        Width = workArea.Width;
        Height = workArea.Height;
        _isWorkAreaMaximized = true;
        RootBorder.Margin = new Thickness(0);
        RootBorder.CornerRadius = new CornerRadius(0);
        MaximizeButton.Content = "\uE923";
    }

    private void RestoreFromWorkArea()
    {
        WindowState = WindowState.Normal;
        Left = _restoreBounds.Left;
        Top = _restoreBounds.Top;
        Width = _restoreBounds.Width;
        Height = _restoreBounds.Height;
        _isWorkAreaMaximized = false;
        RootBorder.Margin = new Thickness(10);
        RootBorder.CornerRadius = new CornerRadius(8);
        MaximizeButton.Content = "\uE922";
    }

    private Rect ResolveCurrentWorkArea()
    {
        var windowHandle = new WindowInteropHelper(this).Handle;
        if (windowHandle == IntPtr.Zero)
            return SystemParameters.WorkArea;

        var monitor = MonitorFromWindow(windowHandle, MonitorDefaultToNearest);
        if (monitor == IntPtr.Zero)
            return SystemParameters.WorkArea;

        var monitorInfo = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(monitor, ref monitorInfo))
            return SystemParameters.WorkArea;

        var work = monitorInfo.WorkArea;
        return new Rect(work.Left, work.Top, work.Right - work.Left, work.Bottom - work.Top);
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr handle, uint flags);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo monitorInfo);

    [StructLayout(LayoutKind.Sequential)]
    private struct RectNative
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MonitorInfo
    {
        public int Size;
        public RectNative MonitorArea;
        public RectNative WorkArea;
        public uint Flags;
    }
}
