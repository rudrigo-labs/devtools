using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DevTools.Host.Wpf.Components;

public enum DevToolsMessageBoxType
{
    Confirmation,
    Info,
    Warning,
    Error
}

public enum DevToolsMessageBoxResult
{
    None,
    Yes,
    No,
    Ok
}

public partial class DevToolsMessageBox : Window
{
    public DevToolsMessageBoxResult Result { get; private set; } = DevToolsMessageBoxResult.None;

    private DevToolsMessageBox(
        Window? owner,
        string title,
        string message,
        DevToolsMessageBoxType type)
    {
        InitializeComponent();

        if (owner is not null)
            Owner = owner;

        TitleText.Text = title;
        MessageText.Text = message;

        ApplyType(type);
        BuildButtons(type);
    }

    // -------------------------------------------------------------------------
    // API estática
    // -------------------------------------------------------------------------

    /// <summary>Exibe diálogo de confirmação (Sim / Não).</summary>
    public static DevToolsMessageBoxResult Confirm(Window? owner, string message, string title = "Confirmação")
        => Show(owner, title, message, DevToolsMessageBoxType.Confirmation);

    /// <summary>Exibe diálogo informativo (OK).</summary>
    public static DevToolsMessageBoxResult Info(Window? owner, string message, string title = "Informação")
        => Show(owner, title, message, DevToolsMessageBoxType.Info);

    /// <summary>Exibe diálogo de aviso (OK).</summary>
    public static DevToolsMessageBoxResult Warning(Window? owner, string message, string title = "Aviso")
        => Show(owner, title, message, DevToolsMessageBoxType.Warning);

    /// <summary>Exibe diálogo de erro (OK).</summary>
    public static DevToolsMessageBoxResult Error(Window? owner, string message, string title = "Erro")
        => Show(owner, title, message, DevToolsMessageBoxType.Error);

    private static DevToolsMessageBoxResult Show(
        Window? owner,
        string title,
        string message,
        DevToolsMessageBoxType type)
    {
        var dialog = new DevToolsMessageBox(owner, title, message, type);
        dialog.ShowDialog();
        return dialog.Result;
    }

    // -------------------------------------------------------------------------
    // Visual por tipo
    // -------------------------------------------------------------------------

    private void ApplyType(DevToolsMessageBoxType type)
    {
        var (icon, color) = type switch
        {
            DevToolsMessageBoxType.Confirmation => ("\uE9CE", "#007ACC"), // ajuda / pergunta
            DevToolsMessageBoxType.Info         => ("\uE946", "#007ACC"), // info
            DevToolsMessageBoxType.Warning      => ("\uE7BA", "#FFA500"), // aviso
            DevToolsMessageBoxType.Error        => ("\uEA39", "#FF4444"), // erro
            _                                   => ("\uE946", "#007ACC")
        };

        IconText.Text = icon;
        IconText.Foreground = new SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color));
    }

    private void BuildButtons(DevToolsMessageBoxType type)
    {
        if (type == DevToolsMessageBoxType.Confirmation)
        {
            ButtonPanel.Children.Add(CreateButton("Não", isAccent: false, onClick: () =>
            {
                Result = DevToolsMessageBoxResult.No;
                Close();
            }));

            ButtonPanel.Children.Add(CreateButton("Sim", isAccent: true, onClick: () =>
            {
                Result = DevToolsMessageBoxResult.Yes;
                Close();
            }));
        }
        else
        {
            ButtonPanel.Children.Add(CreateButton("OK", isAccent: true, onClick: () =>
            {
                Result = DevToolsMessageBoxResult.Ok;
                Close();
            }));
        }
    }

    private static System.Windows.Controls.Button CreateButton(string label, bool isAccent, Action onClick)
    {
        var btn = new System.Windows.Controls.Button
        {
            Content = label,
            MinWidth = 88,
            Height = 34,
            Margin = new Thickness(8, 0, 0, 0),
            Cursor = System.Windows.Input.Cursors.Hand,
            FontSize = 13,
        };

        if (isAccent)
        {
            btn.Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#007ACC"));
            btn.Foreground = System.Windows.Media.Brushes.White;
            btn.BorderThickness = new Thickness(0);
        }
        else
        {
            btn.Background = System.Windows.Media.Brushes.Transparent;
            btn.Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#A0A0A0"));
            btn.BorderBrush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#333333"));
            btn.BorderThickness = new Thickness(1);
        }

        btn.Click += (_, _) => onClick();
        return btn;
    }
}
