using System;
using System.Collections.Generic;
using System.Linq;

namespace DevTools.Presentation.Wpf.ToolRouting;

public sealed class ToolRegistry
{
    private readonly Dictionary<string, ToolDescriptor> _byId = new(StringComparer.OrdinalIgnoreCase);

    public void Register(ToolDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        if (string.IsNullOrWhiteSpace(descriptor.Id))
        {
            throw new ArgumentException("Tool id is required.", nameof(descriptor));
        }

        _byId[descriptor.Id] = descriptor;
    }

    public bool TryGet(string toolId, out ToolDescriptor descriptor) => _byId.TryGetValue(toolId, out descriptor!);

    public IReadOnlyList<ToolDescriptor> GetAllEnabled()
    {
        return _byId.Values
            .Where(tool => tool.IsEnabled)
            .OrderBy(tool => tool.Category)
            .ThenBy(tool => tool.Order)
            .ThenBy(tool => tool.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}

