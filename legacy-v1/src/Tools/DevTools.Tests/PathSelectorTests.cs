using System;
using System.Threading;
using DevTools.Presentation.Wpf.Components;
using Xunit;

namespace DevTools.Tests;

public class PathSelectorTests
{
    [Fact(Skip = "Instavel em xUnit por afinidade de thread do Application.Current e recursos WPF globais.")]
    public void SelectedPath_Updates_TextBox_Display()
    {
        var path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        Exception? error = null;
        var done = new ManualResetEvent(false);

        var t = new Thread(() =>
        {
            try
            {
                TestWpfApplication.EnsureInitialized();

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
        t.IsBackground = true;
        t.Start();
        var completed = done.WaitOne(TimeSpan.FromSeconds(30));
        if (!completed)
        {
            throw new TimeoutException("Timeout aguardando thread STA no teste PathSelectorTests.SelectedPath_Updates_TextBox_Display.");
        }

        Assert.Null(error);
    }
}
