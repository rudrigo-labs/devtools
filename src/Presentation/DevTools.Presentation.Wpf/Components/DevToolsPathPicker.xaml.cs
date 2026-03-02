using System.Windows;
using Microsoft.Win32;

namespace DevTools.Presentation.Wpf.Components;

public partial class DevToolsPathPicker : System.Windows.Controls.UserControl
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(DevToolsPathPicker), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SelectedPathProperty =
        DependencyProperty.Register(nameof(SelectedPath), typeof(string), typeof(DevToolsPathPicker), new PropertyMetadata(string.Empty));

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string SelectedPath
    {
        get => (string)GetValue(SelectedPathProperty);
        set => SetValue(SelectedPathProperty, value);
    }

    public DevToolsPathPicker()
    {
        InitializeComponent();
    }

    private void OnBrowseClick(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Selecione o arquivo",
            Filter = "Todos os arquivos|*.*"
        };
        if (dialog.ShowDialog() == true)
        {
            SelectedPath = dialog.FileName;
        }
    }
}
