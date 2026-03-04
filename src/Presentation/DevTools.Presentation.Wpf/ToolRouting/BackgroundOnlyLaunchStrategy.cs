using System;
using System.Threading.Tasks;

namespace DevTools.Presentation.Wpf.ToolRouting;

public sealed class BackgroundOnlyLaunchStrategy : IToolLaunchStrategy
{
    public ToolLaunchMode Mode => ToolLaunchMode.BackgroundOnly;

    public void Launch(ToolDescriptor descriptor, ToolLaunchContext context)
    {
        if (descriptor.Factory == null)
        {
            context.LogInfo?.Invoke($"Tool '{descriptor.Id}' has no background factory configured.");
            return;
        }

        try
        {
            var result = descriptor.Factory(context.Services);
            if (result is Action action)
            {
                action();
                return;
            }

            if (result is Task task)
            {
                _ = ObserveTaskAsync(task, descriptor, context);
            }
        }
        catch (Exception ex)
        {
            context.LogError?.Invoke($"Background tool '{descriptor.Id}' failed to start.", ex);
        }
    }

    private static async Task ObserveTaskAsync(Task task, ToolDescriptor descriptor, ToolLaunchContext context)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            context.LogError?.Invoke($"Background tool '{descriptor.Id}' failed.", ex);
        }
    }
}

