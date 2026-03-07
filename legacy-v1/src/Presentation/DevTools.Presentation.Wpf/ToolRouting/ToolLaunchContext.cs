using System;
using System.Windows;

namespace DevTools.Presentation.Wpf.ToolRouting;

public sealed class ToolLaunchContext
{
    public required IServiceProvider Services { get; init; }
    public required Func<Window?> GetMainWindow { get; init; }
    public required Action<EmbeddedToolRequest> RequestEmbeddedTool { get; init; }
    public Action<string>? LogInfo { get; init; }
    public Action<string, Exception>? LogError { get; init; }
}

