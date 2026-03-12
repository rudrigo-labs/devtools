using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;

namespace DevTools.Host.Wpf.Views;

public partial class MainWindow : Window
{
    private const uint MonitorDefaultToNearest = 0x00000002;

    // Mapa de tool tag -> UserControl factory. Adicionar novas tools aqui.
    private readonly Dictionary<string, Func<System.Windows.Controls.UserControl>> _toolRegistry;
    private string _activeToolTag = string.Empty;

    public MainWindow(
        SnapshotWorkspaceView snapshotWorkspaceView,
        RenameWorkspaceView renameWorkspaceView,
        HarvestWorkspaceView harvestWorkspaceView)
    {
        InitializeComponent();

        _toolRegistry = new Dictionary<string, Func<System.Windows.Controls.UserControl>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Snapshot"] = () => snapshotWorkspaceView,
            ["Rename"]   = () => renameWorkspaceView,
            ["Harvest"]  = () => harvestWorkspaceView,
        };

        Loaded += (_, _) =>
        {
            WindowState = WindowState.Maximized;
            ActivateTool("Snapshot");
        };
    }

    // -------------------------------------------------------------------------
    // Navegação
    // -------------------------------------------------------------------------

    private void NavButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button btn || btn.Tag is not string tag)
            return;

        ActivateTool(tag);
    }

    private void ActivateTool(string tag)
    {
        if (string.Equals(_activeToolTag, tag, StringComparison.OrdinalIgnoreCase))
            return;

        if (!_toolRegistry.TryGetValue(tag, out var factory))
            return;

        _activeToolTag = tag;
        WorkspaceHost.Content = factory();
        ActiveToolLabel.Text = tag;
        MainStatusText.Text = $"{tag} ativo.";

        UpdateNavStyles();
    }

    private void UpdateNavStyles()
    {
        var activeStyle = TryFindResource("SidebarNavButtonActiveStyle") as Style;
        var normalStyle = TryFindResource("SidebarNavButtonStyle") as Style;

        // Percorrer botões de navegação na sidebar e aplicar estilo conforme ativo.
        foreach (var btn in FindNavButtons())
        {
            var isActive = string.Equals(btn.Tag as string, _activeToolTag, StringComparison.OrdinalIgnoreCase);
            btn.Style = isActive ? activeStyle : normalStyle;
        }
    }

    private IEnumerable<System.Windows.Controls.Button> FindNavButtons()
    {
        // Localiza todos os botões de navegação que possuem Tag de string.
        return FindVisualChildren<System.Windows.Controls.Button>(this)
            .Where(b => b.Tag is string tag && _toolRegistry.ContainsKey(tag));
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent is null) yield break;
        var count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T t) yield return t;
            foreach (var descendant in FindVisualChildren<T>(child))
                yield return descendant;
        }
    }

    // -------------------------------------------------------------------------
    // Shell — drag, minimize, close
    // -------------------------------------------------------------------------

    private void WindowDragArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left) return;
        if (WindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Normal;
            Left = e.GetPosition(this).X - (Width / 2);
            Top = 0;
        }
        try { DragMove(); } catch { }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
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
            RootBorder.Margin = new Thickness(0);
            RootBorder.CornerRadius = new CornerRadius(0);
            MaximizeButton.Content = "\uE923"; // ícone restaurar
        }
        else
        {
            RootBorder.Margin = new Thickness(10);
            RootBorder.CornerRadius = new CornerRadius(8);
            MaximizeButton.Content = "\uE922"; // ícone maximizar
        }
    }

    // -------------------------------------------------------------------------
    // Bounds do monitor corrente
    // -------------------------------------------------------------------------

    private void ApplyWorkAreaBounds()
    {
        var workArea = ResolveCurrentWorkArea();
        WindowState = WindowState.Normal;
        Left = workArea.Left;
        Top = workArea.Top;
        Width = workArea.Width;
        Height = workArea.Height;
        MaxWidth = workArea.Width;
        MaxHeight = workArea.Height;
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
    private struct RectNative { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MonitorInfo
    {
        public int Size;
        public RectNative MonitorArea;
        public RectNative WorkArea;
        public uint Flags;
    }
}
