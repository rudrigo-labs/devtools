using System;
using System.Collections.Generic;

namespace DevTools.Presentation.Wpf.ToolRouting;

public sealed class ToolServiceProvider : IServiceProvider
{
    private readonly Dictionary<Type, object> _services = new();

    public ToolServiceProvider Add<TService>(TService instance) where TService : class
    {
        _services[typeof(TService)] = instance;
        return this;
    }

    public object? GetService(Type serviceType)
    {
        _services.TryGetValue(serviceType, out var instance);
        return instance;
    }
}

