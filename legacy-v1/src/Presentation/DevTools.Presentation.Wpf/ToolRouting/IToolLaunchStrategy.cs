namespace DevTools.Presentation.Wpf.ToolRouting;

public interface IToolLaunchStrategy
{
    ToolLaunchMode Mode { get; }
    void Launch(ToolDescriptor descriptor, ToolLaunchContext context);
}

