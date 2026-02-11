using System;
using System.IO;

namespace DevTools.Presentation.Wpf.Services;

internal static class AppLogger
{
    private static readonly object _lock = new();
    private static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "DevTools",
        "logs");

    public static readonly string LogFilePath = Path.Combine(LogDirectory, "DevTools.Presentation.Wpf.log");

    public static void Info(string message)
    {
        Write("INFO", message, null);
    }

    public static void Error(string message, Exception? ex = null)
    {
        Write("ERROR", message, ex);
    }

    private static void Write(string level, string message, Exception? ex)
    {
        try
        {
            var sb = new System.Text.StringBuilder();
            sb.Append($"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz} [{level}] {message}");

            if (ex != null)
            {
                sb.AppendLine();
                sb.Append(FormatException(ex));
            }

            lock (_lock)
            {
                Directory.CreateDirectory(LogDirectory);
                File.AppendAllText(LogFilePath, sb.ToString() + Environment.NewLine);
            }
        }
        catch
        {
            // Avoid throwing from logger.
        }
    }

    private static string FormatException(Exception ex)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Type: {ex.GetType().FullName}");
        sb.AppendLine($"Message: {ex.Message}");
        sb.AppendLine($"Source: {ex.Source}");
        sb.AppendLine($"StackTrace:");
        sb.AppendLine(ex.StackTrace);

        if (ex is AggregateException agg)
        {
            foreach (var inner in agg.Flatten().InnerExceptions)
            {
                sb.AppendLine("--- Inner Exception (Aggregate) ---");
                sb.Append(FormatException(inner));
            }
        }
        else if (ex.InnerException != null)
        {
            sb.AppendLine("--- Inner Exception ---");
            sb.Append(FormatException(ex.InnerException));
        }

        return sb.ToString();
    }
}
