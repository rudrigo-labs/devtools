using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using DevTools.Core.Configuration;
using DevTools.Notes.Models;
using DevTools.Presentation.Wpf.Components;
using DevTools.Presentation.Wpf.Models;
using DevTools.Presentation.Wpf.Services;
using DevTools.Presentation.Wpf.Views;
using DevTools.SSHTunnel.Models;

namespace DevTools.Tests;

public class ToolUsageSimulationTests
{
    [Fact]
    public void TrayRouter_OpenAllTools_AndCoreWorkflows_RunWithoutCrash()
    {
        var stage = "startup";
        RunInSta(() =>
        {
            EnsureApplicationWithTheme();

            var tempRoot = Path.Combine(Path.GetTempPath(), "devtools-integration-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);

            try
            {
                var jobManager = new JobManager();
                var settings = new SettingsService();
                var config = new ConfigService();
                var profileManager = new ProfileManager();
                var gdrive = new GoogleDriveService();

                if (CanRunTrayRouterSmoke())
                {
                    stage = "RunTrayRouterSmoke";
                    RunTrayRouterSmoke(jobManager, settings, config, profileManager, gdrive, s => stage = s);
                }
                else
                {
                    stage = "RunTrayRouterSmoke:SkippedDispatcherThreadMismatch";
                }

                stage = "RunOrganizerFlow";
                RunOrganizerFlow(jobManager, settings, tempRoot);
                stage = "RunUtf8Flow";
                RunUtf8Flow(jobManager, settings, tempRoot);
                stage = "RunSnapshotFlow";
                RunSnapshotFlow(jobManager, settings, profileManager, tempRoot);
                stage = "RunSearchTextFlow";
                RunSearchTextFlow(jobManager, settings, profileManager, tempRoot);
                stage = "RunRenameFlow";
                RunRenameFlow(jobManager, settings, profileManager, tempRoot);
                stage = "RunHarvestFlow";
                RunHarvestFlow(jobManager, settings, profileManager, tempRoot);
                stage = "RunImageSplitFlow";
                RunImageSplitFlow(jobManager, settings, tempRoot);
                stage = "RunMigrationsFlow";
                RunMigrationsFlow(jobManager, settings, config, profileManager, tempRoot);
                stage = "RunNotesFlow";
                RunNotesFlow(settings, config, gdrive, tempRoot);
                stage = "RunAuxiliaryWindowsFlow";
                RunAuxiliaryWindowsFlow(jobManager, settings, config, profileManager);

                Assert.True(true);
            }
            finally
            {
                SafeCloseAllWindows();
                SafeDeleteDirectory(tempRoot);
            }
        }, () => stage);
    }

    private static void RunTrayRouterSmoke(
        JobManager jobManager,
        SettingsService settings,
        ConfigService config,
        ProfileManager profileManager,
        GoogleDriveService gdrive,
        Action<string>? updateStage = null)
    {
        var tray = new TrayService(jobManager, settings, config, profileManager, gdrive);

        var ids = new[]
        {
            "Dashboard", "Jobs", "Logs", "Notes", "Organizer", "Harvest", "SearchText",
            "Migrations", "ImageSplitter", "Rename", "Utf8Convert", "Snapshot", "SSHTunnel", "Ngrok"
        };

        foreach (var id in ids)
        {
            updateStage?.Invoke($"RunTrayRouterSmoke:{id}");
            tray.OpenTool(id);
            PumpDispatcher(3);
            tray.OpenTool("HIDE_CURRENT");
            PumpDispatcher(2);
        }
    }

    private static void RunOrganizerFlow(JobManager jobManager, SettingsService settings, string tempRoot)
    {
        var input = Path.Combine(tempRoot, "organizer-input");
        Directory.CreateDirectory(input);
        File.WriteAllText(Path.Combine(input, "sample.txt"), "hello organizer");

        var window = new OrganizerWindow(jobManager, settings);
        window.Show();
        PumpDispatcher(3);

        GetField<PathSelector>(window, "InputPathSelector").SelectedPath = input;
        GetField<PathSelector>(window, "OutputPathSelector").SelectedPath = Path.Combine(tempRoot, "organizer-output");

        Invoke(window, "RunButton_Click", window, new RoutedEventArgs());
        WaitForJobs(jobManager);
        Assert.Contains(jobManager.Jobs, j => string.Equals(j.Name, "Organizer", StringComparison.OrdinalIgnoreCase));
    }

    private static void RunUtf8Flow(JobManager jobManager, SettingsService settings, string tempRoot)
    {
        var root = Path.Combine(tempRoot, "utf8-root");
        Directory.CreateDirectory(root);
        var file = Path.Combine(root, "legacy.txt");
        File.WriteAllText(file, "conteudo utf8");

        var window = new Utf8ConvertWindow(jobManager, settings);
        window.Show();
        PumpDispatcher(3);

        GetField<PathSelector>(window, "RootPathSelector").SelectedPath = root;
        Invoke(window, "ProcessButton_Click", window, new RoutedEventArgs());

        WaitForJobs(jobManager);
        Assert.True(File.Exists(file));
    }

    private static void RunSnapshotFlow(JobManager jobManager, SettingsService settings, ProfileManager profileManager, string tempRoot)
    {
        var root = Path.Combine(tempRoot, "snapshot-root");
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "Program.cs"), "class Program {}");

        var window = new SnapshotWindow(jobManager, settings, profileManager);
        window.Show();
        PumpDispatcher(3);

        GetField<PathSelector>(window, "RootPathSelector").SelectedPath = root;
        GetField<CheckBox>(window, "TextCheck").IsChecked = true;
        Invoke(window, "ProcessButton_Click", window, new RoutedEventArgs());

        WaitForJobs(jobManager);
        var snapshotFolder = Path.Combine(root, "Snapshot");
        Assert.True(Directory.Exists(snapshotFolder));
    }

    private static void RunSearchTextFlow(JobManager jobManager, SettingsService settings, ProfileManager profileManager, string tempRoot)
    {
        var root = Path.Combine(tempRoot, "search-root");
        Directory.CreateDirectory(root);
        var token = "TOKEN_FIND_ME_123";
        File.WriteAllText(Path.Combine(root, "search.txt"), token + Environment.NewLine + "other line");

        var window = new SearchTextWindow(jobManager, settings, profileManager);
        window.Show();
        PumpDispatcher(3);

        GetField<PathSelector>(window, "PathSelector").SelectedPath = root;
        GetField<TextBox>(window, "SearchTextInput").Text = token;
        Invoke(window, "Execute_Click", window, new RoutedEventArgs());

        WaitForJobs(jobManager);
        PumpDispatcher(4);

        var output = GetField<TextBox>(window, "OutputText").Text;
        Assert.False(string.IsNullOrWhiteSpace(output));
        Assert.DoesNotContain("Buscando...", output, StringComparison.OrdinalIgnoreCase);
        window.Close();
    }

    private static void RunRenameFlow(JobManager jobManager, SettingsService settings, ProfileManager profileManager, string tempRoot)
    {
        var root = Path.Combine(tempRoot, "rename-root");
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "OldNameService.txt"), "OldNameService");

        var window = new RenameWindow(jobManager, settings, profileManager);
        window.Show();
        PumpDispatcher(3);

        GetField<PathSelector>(window, "RootPathSelector").SelectedPath = root;
        GetField<TextBox>(window, "OldTextBox").Text = "OldName";
        GetField<TextBox>(window, "NewTextBox").Text = "NewName";
        GetField<CheckBox>(window, "RememberSettingsCheck").IsChecked = false;

        Invoke(window, "ProcessButton_Click", window, new RoutedEventArgs());
        WaitUntil(() => window.IsEnabled, timeoutMs: 12000);

        var hasRenamedFile = Directory.GetFiles(root, "*", SearchOption.AllDirectories)
            .Any(path => Path.GetFileName(path).Contains("NewName", StringComparison.OrdinalIgnoreCase));
        Assert.True(hasRenamedFile);
        window.Close();
    }

    private static void RunHarvestFlow(JobManager jobManager, SettingsService settings, ProfileManager profileManager, string tempRoot)
    {
        var source = Path.Combine(tempRoot, "harvest-source");
        var output = Path.Combine(tempRoot, "harvest-output");
        Directory.CreateDirectory(source);
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(source, "Service.cs"), "public class SecurityHelper { void Encrypt(){} }");

        var window = new HarvestWindow(jobManager, settings, profileManager);
        window.Show();
        PumpDispatcher(3);

        GetField<PathSelector>(window, "SourcePathSelector").SelectedPath = source;
        GetField<PathSelector>(window, "OutputPathSelector").SelectedPath = output;
        GetField<TextBox>(window, "MinScoreBox").Text = "0";
        GetField<CheckBox>(window, "RememberSettingsCheck").IsChecked = false;

        Invoke(window, "Run_Click", window, new RoutedEventArgs());
        WaitUntil(() => window.IsEnabled, timeoutMs: 12000);
        window.Close();
    }

    private static void RunImageSplitFlow(JobManager jobManager, SettingsService settings, string tempRoot)
    {
        var imageRoot = Path.Combine(tempRoot, "image-split");
        var output = Path.Combine(imageRoot, "out");
        Directory.CreateDirectory(imageRoot);
        Directory.CreateDirectory(output);

        var inputImage = Path.Combine(imageRoot, "sample.png");
        using (var bitmap = new System.Drawing.Bitmap(20, 20))
        {
            using var g = System.Drawing.Graphics.FromImage(bitmap);
            g.Clear(System.Drawing.Color.White);
            bitmap.Save(inputImage, System.Drawing.Imaging.ImageFormat.Png);
        }

        var window = new ImageSplitWindow(jobManager, settings);
        window.Show();
        PumpDispatcher(3);

        GetField<PathSelector>(window, "InputPathSelector").SelectedPath = inputImage;
        GetField<PathSelector>(window, "OutputPathSelector").SelectedPath = output;
        GetField<TextBox>(window, "MinSizeBox").Text = "1";
        GetField<TextBox>(window, "AlphaBox").Text = "10";
        GetField<CheckBox>(window, "OverwriteCheck").IsChecked = true;

        Invoke(window, "ProcessButton_Click", window, new RoutedEventArgs());
        WaitForJobs(jobManager);

        Assert.True(Directory.Exists(output));
    }

    private static void RunMigrationsFlow(JobManager jobManager, SettingsService settings, ConfigService config, ProfileManager profileManager, string tempRoot)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "src"));
        var startupCsproj = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..",
            "src", "Tools", "DevTools.Tests", "DevTools.Tests.csproj"));

        var window = new MigrationsWindow(jobManager, settings, config, profileManager);
        window.Show();
        PumpDispatcher(3);

        GetField<PathSelector>(window, "ProjectSelector").SelectedPath = root;
        GetField<PathSelector>(window, "StartupSelector").SelectedPath = startupCsproj;
        GetField<TextBox>(window, "DbContextInput").Text = "Sample.DbContext";

        // Update Database (na implementação atual: qualquer ação != Add é tratada como update)
        GetField<ComboBox>(window, "ActionCombo").SelectedIndex = 2;
        GetField<CheckBox>(window, "DryRunCheck").IsChecked = true;

        Invoke(window, "Execute_Click", window, new RoutedEventArgs());
        WaitForJobs(jobManager);

        var output = GetField<TextBox>(window, "OutputText").Text;
        Assert.False(string.IsNullOrWhiteSpace(output));
        window.Close();
    }

    private static void RunNotesFlow(SettingsService settings, ConfigService config, GoogleDriveService gdrive, string tempRoot)
    {
        var notesRoot = Path.Combine(tempRoot, "notes-root");
        Directory.CreateDirectory(notesRoot);

        config.SaveSection("Notes", new NotesSettings
        {
            StoragePath = notesRoot,
            DefaultFormat = ".txt",
            AutoCloudSync = false
        });

        config.SaveSection("GoogleDrive", new GoogleDriveSettings
        {
            IsEnabled = false
        });

        var window = new NotesWindow(settings, gdrive, config);
        window.Show();
        PumpDispatcher(6);

        Invoke(window, "AddButton_Click", window, new RoutedEventArgs());
        GetField<TextBox>(window, "NoteTitle").Text = "IntegrationNote";
        GetField<TextBox>(window, "NotesContent").Text = "conteudo de teste";
        Invoke(window, "SaveButton_Click", window, new RoutedEventArgs());

        WaitUntil(
            () => Directory.Exists(Path.Combine(notesRoot, "items"))
                  && (Directory.GetFiles(Path.Combine(notesRoot, "items"), "*.txt", SearchOption.AllDirectories).Any()
                      || Directory.GetFiles(Path.Combine(notesRoot, "items"), "*.md", SearchOption.AllDirectories).Any()),
            timeoutMs: 12000);

        var list = GetField<ListBox>(window, "NotesList");
        WaitUntil(() => list.Items.Count > 0, timeoutMs: 12000);
        var first = Assert.IsType<NoteListItem>(list.Items[0]);

        UiMessageService.ConfirmOverrideForTests = (_, _) => true;
        try
        {
            if (Invoke(window, "DeleteNoteByKeyAsync", first.FileName, first.Title) is Task deleteTask)
            {
                deleteTask.GetAwaiter().GetResult();
            }
        }
        finally
        {
            UiMessageService.ConfirmOverrideForTests = null;
        }

        var deletedPath = Path.Combine(notesRoot, "items", first.FileName.Replace('/', Path.DirectorySeparatorChar));
        WaitUntil(() => !File.Exists(deletedPath), timeoutMs: 12000);
        WaitUntil(() => list.Items.Cast<object>().All(i => !string.Equals(((NoteListItem)i).FileName, first.FileName, StringComparison.OrdinalIgnoreCase)), timeoutMs: 12000);
        window.Close();
    }

    private static void RunAuxiliaryWindowsFlow(JobManager jobManager, SettingsService settings, ConfigService config, ProfileManager profileManager)
    {
        var logs = new LogsWindow(settings);
        logs.Show();
        PumpDispatcher(3);
        Invoke(logs, "Refresh_Click", logs, new RoutedEventArgs());
        Invoke(logs, "Clear_Click", logs, new RoutedEventArgs());
        logs.Close();

        var ngrok = new NgrokWindow(settings);
        ngrok.Show();
        PumpDispatcher(4);
        Invoke(ngrok, "RefreshButton_Click", ngrok, new RoutedEventArgs());
        PumpDispatcher(4);
        ngrok.Close();

        var ssh = new SshTunnelWindow(jobManager, settings, config, profileManager);
        ssh.Show();
        PumpDispatcher(3);
        GetField<TextBox>(ssh, "SshHostInput").Text = "example.local";
        GetField<TextBox>(ssh, "SshPortInput").Text = "22";
        GetField<TextBox>(ssh, "SshUserInput").Text = "user";
        GetField<TextBox>(ssh, "LocalBindInput").Text = "127.0.0.1";
        GetField<TextBox>(ssh, "LocalPortInput").Text = "5432";
        GetField<TextBox>(ssh, "RemoteHostInput").Text = "127.0.0.1";
        GetField<TextBox>(ssh, "RemotePortInput").Text = "5432";
        var profile = (TunnelProfile)Invoke(ssh, "BuildProfileFromUi")!;
        Assert.Equal("example.local", profile.SshHost);
        ssh.Close();

        var help = new HelpWindow();
        help.Show();
        PumpDispatcher(2);
        help.Close();
    }

    private static void RunInSta(Action action, Func<string>? stageProvider = null)
    {
        Exception? captured = null;
        var done = new ManualResetEvent(false);

        var thread = new Thread(() =>
        {
            try
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
                action();
            }
            catch (Exception ex)
            {
                captured = ex;
            }
            finally
            {
                done.Set();
                Dispatcher.CurrentDispatcher.InvokeShutdown();
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        var completed = done.WaitOne(TimeSpan.FromMinutes(3));
        if (!completed)
        {
            try
            {
                Dispatcher.FromThread(thread)?.BeginInvokeShutdown(DispatcherPriority.Send);
            }
            catch
            {
                // ignore cleanup errors
            }

            var stage = stageProvider?.Invoke() ?? "desconhecido";
            captured ??= new TimeoutException($"Timeout aguardando thread STA no teste ToolUsageSimulationTests.TrayRouter_OpenAllTools_AndCoreWorkflows_RunWithoutCrash. Etapa: {stage}.");
        }

        Assert.Null(captured);
    }

    private static void EnsureApplicationWithTheme()
    {
        TestWpfApplication.EnsureInitialized();
    }

    private static bool CanRunTrayRouterSmoke()
    {
        var app = Application.Current;
        if (app == null)
        {
            return true;
        }

        return app.Dispatcher.CheckAccess();
    }

    private static void WaitForJobs(JobManager manager, int timeoutMs = 15000)
    {
        WaitUntil(() => manager.RunningJobsCount == 0, timeoutMs);
    }

    private static void WaitUntil(Func<bool> predicate, int timeoutMs = 8000)
    {
        var started = DateTime.UtcNow;
        while ((DateTime.UtcNow - started).TotalMilliseconds < timeoutMs)
        {
            PumpDispatcher(1);
            if (predicate())
            {
                return;
            }

            Thread.Sleep(50);
        }

        throw new TimeoutException("Condition was not reached before timeout.");
    }

    private static void PumpDispatcher(int cycles)
    {
        for (var i = 0; i < cycles; i++)
        {
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new DispatcherOperationCallback(_ =>
                {
                    frame.Continue = false;
                    return null;
                }),
                null);
            Dispatcher.PushFrame(frame);
        }
    }

    private static T GetField<T>(object target, string name) where T : class
    {
        var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        var field = target.GetType().GetField(name, flags);
        if (field?.GetValue(target) is not T value)
        {
            throw new InvalidOperationException($"Field '{name}' not found on {target.GetType().Name}.");
        }

        return value;
    }

    private static object? Invoke(object target, string method, params object?[]? args)
    {
        var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        var mi = target.GetType().GetMethod(method, flags);
        if (mi == null)
        {
            throw new InvalidOperationException($"Method '{method}' not found on {target.GetType().Name}.");
        }

        return mi.Invoke(target, args);
    }

    private static void SafeCloseAllWindows()
    {
        var app = Application.Current;
        if (app == null)
        {
            return;
        }

        void CloseWindowsCore()
        {
            foreach (var window in app.Windows.OfType<Window>().ToList())
            {
                try
                {
                    window.Close();
                }
                catch
                {
                    // ignore
                }
            }
        }

        if (app.Dispatcher.CheckAccess())
        {
            CloseWindowsCore();
            PumpDispatcher(2);
            return;
        }

        app.Dispatcher.Invoke(CloseWindowsCore);
    }

    private static void SafeDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
        catch
        {
            // ignore temp cleanup issues
        }
    }
}
