using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace DevTools.Presentation.Wpf.Components;

public partial class PathSelector : System.Windows.Controls.UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register("Title", typeof(string), typeof(PathSelector), new PropertyMetadata("Path"));

    public static readonly DependencyProperty SelectedPathProperty =
        DependencyProperty.Register("SelectedPath", typeof(string), typeof(PathSelector), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IsFolderPickerProperty =
        DependencyProperty.Register("IsFolderPicker", typeof(bool), typeof(PathSelector), new PropertyMetadata(true));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string SelectedPath
    {
        get => (string)GetValue(SelectedPathProperty);
        set => SetValue(SelectedPathProperty, value);
    }

    public bool IsFolderPicker
    {
        get => (bool)GetValue(IsFolderPickerProperty);
        set => SetValue(IsFolderPickerProperty, value);
    }

    public PathSelector()
    {
        InitializeComponent();
        TitleBlock.DataContext = this;
        PathInput.DataContext = this;
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        if (IsFolderPicker)
        {
            var dialog = new OpenFolderDialog();
            if (dialog.ShowDialog() == true)
            {
                SelectedPath = dialog.FolderName;
                PathInput.Text = SelectedPath;
            }
        }
        else
        {
            var dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == true)
            {
                SelectedPath = dialog.FileName;
                PathInput.Text = SelectedPath;
            }
        }
    }
}