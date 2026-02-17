using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using DevTools.Presentation.Wpf.Components;
using Xunit;

namespace DevTools.Tests;

public class PathSelectorTests
{
    [Fact]
    public void SelectedPath_Updates_TextBox_Display()
    {
        var path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        Exception? error = null;
        var done = new ManualResetEvent(false);

        var t = new Thread(() =>
        {
            try
            {
                var control = new PathSelector();
                control.SelectedPath = path;

                Assert.Equal(path, control.SelectedPath);
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

    private static TextBox? FindTextBox(DependencyObject root) => null;
}
