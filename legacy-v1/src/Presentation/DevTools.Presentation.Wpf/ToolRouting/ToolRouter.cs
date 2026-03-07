using System.Collections.Generic;

namespace DevTools.Presentation.Wpf.ToolRouting;

public sealed class ToolRouter
{
    private readonly ToolRegistry _registry;
    private readonly Dictionary<ToolLaunchMode, IToolLaunchStrategy> _strategies = new();

    public ToolRouter(ToolRegistry registry, IEnumerable<IToolLaunchStrategy> strategies)
    {
        _registry = registry;
        foreach (var strategy in strategies)
        {
            _strategies[strategy.Mode] = strategy;
        }
    }

    public bool TryOpen(string toolId, ToolLaunchContext context)
    {
        if (!_registry.TryGet(toolId, out var descriptor))
        {
            return false;
        }

        if (!descriptor.IsEnabled)
        {
            context.LogInfo?.Invoke($"Tool disabled and ignored: {toolId}");
            return true;
        }

        if (!_strategies.TryGetValue(descriptor.LaunchMode, out var strategy))
        {
            context.LogInfo?.Invoke($"No launch strategy available for tool '{descriptor.Id}' ({descriptor.LaunchMode}).");
            return true;
        }

        strategy.Launch(descriptor, context);
        return true;
    }
}

