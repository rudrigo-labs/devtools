using System;
using System.Diagnostics;

namespace DevTools.Infrastructure.Diagnostics;

internal static class InfraLogger
{
    public static void Info(string message)
    {
        try
        {
            Debug.WriteLine($"[DevTools.Infrastructure][INFO] {message}");
        }
        catch
        {
            // no-op
        }
    }

    public static void Error(string message, Exception? ex = null)
    {
        try
        {
            var details = ex == null ? string.Empty : $" {ex}";
            Debug.WriteLine($"[DevTools.Infrastructure][ERROR] {message}{details}");
        }
        catch
        {
            // no-op
        }
    }
}
