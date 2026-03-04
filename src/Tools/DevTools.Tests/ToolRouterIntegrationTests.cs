using System;
using System.Collections.Generic;
using DevTools.Presentation.Wpf.ToolRouting;

namespace DevTools.Tests;

public class ToolRouterIntegrationTests
{
    [Fact]
    public void TryOpen_UnknownTool_ReturnsFalse()
    {
        var router = new ToolRouter(new ToolRegistry(), Array.Empty<IToolLaunchStrategy>());
        var context = CreateContext(_ => { });

        var opened = router.TryOpen("unknown-tool", context);

        Assert.False(opened);
    }

    [Fact]
    public void TryOpen_DisabledTool_DoesNotLaunch_AndLogsInfo()
    {
        var logs = new List<string>();
        var registry = new ToolRegistry();
        registry.Register(new ToolDescriptor
        {
            Id = "logs",
            Title = "Logs",
            LaunchMode = ToolLaunchMode.EmbeddedTab,
            IsEnabled = false
        });

        var router = new ToolRouter(registry, new IToolLaunchStrategy[] { new EmbeddedTabLaunchStrategy() });
        var embeddedCalls = 0;
        var context = CreateContext(_ => embeddedCalls++, logs);

        var opened = router.TryOpen("logs", context);

        Assert.True(opened);
        Assert.Equal(0, embeddedCalls);
        Assert.Contains(logs, message => message.Contains("Tool disabled and ignored", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void EmbeddedTab_Singleton_ReusesSameContent()
    {
        var registry = new ToolRegistry();
        var factoryCalls = 0;
        registry.Register(new ToolDescriptor
        {
            Id = "embedded-logs",
            Title = "Embedded Logs",
            LaunchMode = ToolLaunchMode.EmbeddedTab,
            Singleton = true,
            Factory = _ =>
            {
                factoryCalls++;
                return new object();
            }
        });

        var router = new ToolRouter(registry, new IToolLaunchStrategy[] { new EmbeddedTabLaunchStrategy() });
        EmbeddedToolRequest? first = null;
        EmbeddedToolRequest? second = null;
        var call = 0;
        var context = CreateContext(request =>
        {
            call++;
            if (call == 1)
            {
                first = request;
            }
            else
            {
                second = request;
            }
        });

        router.TryOpen("embedded-logs", context);
        router.TryOpen("embedded-logs", context);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(1, factoryCalls);
        Assert.Same(first!.Content, second!.Content);
    }

    [Fact]
    public void EmbeddedTab_NonSingleton_CreatesNewContentOnEachLaunch()
    {
        var registry = new ToolRegistry();
        var factoryCalls = 0;
        registry.Register(new ToolDescriptor
        {
            Id = "live-status",
            Title = "Live Status",
            LaunchMode = ToolLaunchMode.EmbeddedTab,
            Singleton = false,
            Factory = _ =>
            {
                factoryCalls++;
                return new object();
            }
        });

        var router = new ToolRouter(registry, new IToolLaunchStrategy[] { new EmbeddedTabLaunchStrategy() });
        object? firstContent = null;
        object? secondContent = null;
        var call = 0;
        var context = CreateContext(request =>
        {
            call++;
            if (call == 1)
            {
                firstContent = request.Content;
            }
            else
            {
                secondContent = request.Content;
            }
        });

        router.TryOpen("live-status", context);
        router.TryOpen("live-status", context);

        Assert.Equal(2, factoryCalls);
        Assert.NotNull(firstContent);
        Assert.NotNull(secondContent);
        Assert.NotSame(firstContent, secondContent);
    }

    [Fact]
    public void BackgroundOnly_ExecutesActionFromFactory()
    {
        var registry = new ToolRegistry();
        var runs = 0;
        registry.Register(new ToolDescriptor
        {
            Id = "background-sync",
            Title = "Background Sync",
            LaunchMode = ToolLaunchMode.BackgroundOnly,
            Factory = _ => (Action)(() => runs++)
        });

        var router = new ToolRouter(registry, new IToolLaunchStrategy[] { new BackgroundOnlyLaunchStrategy() });
        var context = CreateContext(_ => { });

        var opened = router.TryOpen("background-sync", context);

        Assert.True(opened);
        Assert.Equal(1, runs);
    }

    private static ToolLaunchContext CreateContext(Action<EmbeddedToolRequest> onEmbedded, List<string>? infoLogs = null)
    {
        return new ToolLaunchContext
        {
            Services = new ToolServiceProvider(),
            GetMainWindow = () => null,
            RequestEmbeddedTool = onEmbedded,
            LogInfo = message =>
            {
                infoLogs?.Add(message);
            },
            LogError = (_, _) => { }
        };
    }
}

