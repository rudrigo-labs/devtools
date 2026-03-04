using System;
using System.Collections.Generic;

namespace DevTools.Presentation.Wpf.ToolRouting;

public sealed class EmbeddedTabLaunchStrategy : IToolLaunchStrategy
{
    private readonly Dictionary<string, object?> _singletonContent = new(StringComparer.OrdinalIgnoreCase);

    public ToolLaunchMode Mode => ToolLaunchMode.EmbeddedTab;

    public void Launch(ToolDescriptor descriptor, ToolLaunchContext context)
    {
        object? content = null;

        if (descriptor.Singleton && _singletonContent.TryGetValue(descriptor.Id, out var cachedContent))
        {
            content = cachedContent;
        }
        else if (descriptor.Factory != null)
        {
            content = descriptor.Factory(context.Services);
            if (descriptor.Singleton)
            {
                _singletonContent[descriptor.Id] = content;
            }
        }

        context.RequestEmbeddedTool(new EmbeddedToolRequest(descriptor, content));
    }
}

