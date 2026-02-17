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
        Exception? error = null;
        var done = new ManualResetEvent(false);
        var path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var textFromUi = string.Empty;

        var t = new Thread(() =>
        {
            try
            {
                var control = new PathSelector();
                control.SelectedPath = path;
                control.ApplyTemplate();
                control.UpdateLayout();

                var field = typeof(PathSelector).GetField("PathInput", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var inner = (TextBox?)field?.GetValue(control);
                textFromUi = inner?.Text ?? string.Empty;
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
        Assert.Equal(path, textFromUi);
    }

    private static TextBox? FindTextBox(DependencyObject root) => null;
}
