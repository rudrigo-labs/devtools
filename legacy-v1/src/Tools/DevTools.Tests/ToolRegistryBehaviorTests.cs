using DevTools.Presentation.Wpf.ToolRouting;

namespace DevTools.Tests;

public class ToolRegistryBehaviorTests
{
    [Fact]
    public void GetAllEnabled_FiltersDisabledAndSortsByCategoryOrderThenTitle()
    {
        var registry = new ToolRegistry();

        registry.Register(new ToolDescriptor
        {
            Id = "zeta",
            Title = "Zeta",
            LaunchMode = ToolLaunchMode.DetachedWindow,
            Category = ToolCategory.Productivity,
            Order = 30,
            IsEnabled = true
        });

        registry.Register(new ToolDescriptor
        {
            Id = "alpha-nav",
            Title = "Alpha Navigation",
            LaunchMode = ToolLaunchMode.EmbeddedTab,
            Category = ToolCategory.Navigation,
            Order = 10,
            IsEnabled = true
        });

        registry.Register(new ToolDescriptor
        {
            Id = "beta-nav",
            Title = "Beta Navigation",
            LaunchMode = ToolLaunchMode.EmbeddedTab,
            Category = ToolCategory.Navigation,
            Order = 10,
            IsEnabled = true
        });

        registry.Register(new ToolDescriptor
        {
            Id = "disabled",
            Title = "Disabled Tool",
            LaunchMode = ToolLaunchMode.DetachedWindow,
            Category = ToolCategory.Infrastructure,
            Order = 1,
            IsEnabled = false
        });

        var enabled = registry.GetAllEnabled();
        var ids = enabled.Select(x => x.Id).ToArray();

        Assert.Equal(3, enabled.Count);
        Assert.Equal(new[] { "alpha-nav", "beta-nav", "zeta" }, ids);
    }

    [Fact]
    public void Register_SameId_OverridesDescriptor()
    {
        var registry = new ToolRegistry();

        registry.Register(new ToolDescriptor
        {
            Id = "logs",
            Title = "Logs V1",
            LaunchMode = ToolLaunchMode.EmbeddedTab,
            Category = ToolCategory.Diagnostics,
            Order = 10
        });

        registry.Register(new ToolDescriptor
        {
            Id = "logs",
            Title = "Logs V2",
            LaunchMode = ToolLaunchMode.DetachedWindow,
            Category = ToolCategory.Productivity,
            Order = 99
        });

        var found = registry.TryGet("logs", out var descriptor);

        Assert.True(found);
        Assert.Equal("Logs V2", descriptor.Title);
        Assert.Equal(ToolLaunchMode.DetachedWindow, descriptor.LaunchMode);
        Assert.Equal(ToolCategory.Productivity, descriptor.Category);
        Assert.Equal(99, descriptor.Order);
    }
}

