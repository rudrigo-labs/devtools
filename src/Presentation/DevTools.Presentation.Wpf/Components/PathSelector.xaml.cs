using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace DevTools.Presentation.Wpf.Components;

public partial class PathSelector : System.Windows.Controls.UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register("Title", typeof(string), typeof(PathSelector), new PropertyMetadata("Path", OnTitleChanged));

    public static readonly DependencyProperty SelectedPathProperty =
        DependencyProperty.Register("SelectedPath", typeof(string), typeof(PathSelector), new PropertyMetadata(string.Empty, OnSelectedPathChanged));

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
    }

    private static void OnSelectedPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (PathSelector)d;
        control.PathInput.Text = e.NewValue as string ?? string.Empty;
    }

    private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (PathSelector)d;
        control.TitleBlock.Text = e.NewValue as string ?? "Path";
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        if (IsFolderPicker)
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Selecione a pasta",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = true
            };

            var result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
            {
                SelectedPath = dialog.SelectedPath;
                PathInput.Text = SelectedPath;
            }
        }
        else
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            var owner = Window.GetWindow(this);
            if (dialog.ShowDialog(owner) == true)
            {
                SelectedPath = dialog.FileName;
                PathInput.Text = SelectedPath;
            }
        }
    }
}
