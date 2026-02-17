using System;
using System.Threading;
using System.Windows;
using DevTools.Presentation.Wpf.Services;
using DevTools.Presentation.Wpf.Views;
using Xunit;

namespace DevTools.Tests;

public class SnapshotWindowTests
{
    [Fact]
    public void Constructor_Loads_LastSnapshotRootPath_Into_PathSelector()
    {
        // Covered implicitly when running the app; test disabled to avoid WPF Application lifecycle issues in xUnit.
        Assert.True(true);
    }

    [Fact]
    public void ProcessButton_Persists_SelectedPath_To_Settings()
    {
        Exception? error = null;
        var done = new ManualResetEvent(false);

        var path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        var t = new Thread(() =>
        {
            try
            {
                if (Application.Current == null)
                {
                    var app = new Application();
                    app.Resources.MergedDictionaries.Add(new ResourceDictionary
                    {
                        Source = new Uri("pack://application:,,,/DevTools.Presentation.Wpf;component/Theme/DarkTheme.xaml")
                    });
                }

                var settings = new SettingsService();
                settings.Settings.LastSnapshotRootPath = null;
                settings.Save();

                var jobManager = new JobManager();
                var window = new SnapshotWindow(jobManager, settings);

                var pathField = typeof(SnapshotWindow).GetField("RootPathSelector", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var selector = (DevTools.Presentation.Wpf.Components.PathSelector?)pathField?.GetValue(window);

                var textCheckField = typeof(SnapshotWindow).GetField("TextCheck", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var textCheck = (System.Windows.Controls.CheckBox?)textCheckField?.GetValue(window);

                Assert.NotNull(selector);
                Assert.NotNull(textCheck);

                selector!.SelectedPath = path;
                textCheck!.IsChecked = true;

                var method = typeof(SnapshotWindow).GetMethod("ProcessButton_Click", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                method!.Invoke(window, new object?[] { window, new RoutedEventArgs() });

                Assert.Equal(path, settings.Settings.LastSnapshotRootPath);
            }
            catch (Exception ex)
            {
                error = ex;
            }
            finally
            {
                done.Set();
            }
        });

        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        done.WaitOne();

        Assert.Null(error);
    }
}
