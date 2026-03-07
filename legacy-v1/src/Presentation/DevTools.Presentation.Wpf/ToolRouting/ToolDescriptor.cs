using System;

namespace DevTools.Presentation.Wpf.ToolRouting;

public sealed class ToolDescriptor
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required ToolLaunchMode LaunchMode { get; init; }
    public Func<IServiceProvider, object?>? Factory { get; init; }
    public string? Subtitle { get; init; }
    public string? IconKey { get; init; }
    public ToolCategory Category { get; init; } = ToolCategory.Productivity;
    public int Order { get; init; }
    public bool Singleton { get; init; } = true;
    public bool IsEnabled { get; init; } = true;
    public EmbeddedToolTarget EmbeddedTarget { get; init; } = EmbeddedToolTarget.EmbeddedHost;
}

