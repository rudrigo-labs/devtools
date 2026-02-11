using System.Windows;
using DevTools.Presentation.Wpf.Services;
using DevTools.Presentation.Wpf.Models;

namespace DevTools.Presentation.Wpf.Views;

public partial class JobCenterWindow : Window
{
    private readonly JobManager _jobManager;

    public JobCenterWindow(JobManager jobManager)
    {
        InitializeComponent();
        _jobManager = jobManager;
        DataContext = _jobManager;
    }

    private void Header_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // DragMove();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
        => Close();

    private void CancelSelected_Click(object sender, RoutedEventArgs e)
    {
        if (JobsGrid.SelectedItem is UiJob job)
        {
            _jobManager.CancelJob(job.Id);
        }
    }
}
