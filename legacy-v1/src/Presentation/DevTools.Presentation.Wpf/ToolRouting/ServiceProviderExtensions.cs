using System;

namespace DevTools.Presentation.Wpf.ToolRouting;

public static class ServiceProviderExtensions
{
    public static T GetRequiredService<T>(this IServiceProvider services) where T : class
    {
        return services.GetService(typeof(T)) as T
            ?? throw new InvalidOperationException($"Service '{typeof(T).Name}' is not registered for tool factory.");
    }
}

