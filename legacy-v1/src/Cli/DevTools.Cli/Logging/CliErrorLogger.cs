using System.Text;
using DevTools.Core.Results;

namespace DevTools.Cli.Logging;

internal static class CliErrorLogger
{
    private static readonly object Sync = new();

    public static void LogErrors(string toolKey, IReadOnlyList<ErrorDetail> errors)
    {
        if (errors is null || errors.Count == 0)
            return;

        foreach (var error in errors)
            LogLine(toolKey, FormatError(error));
    }

    public static void LogException(string toolKey, Exception ex)
    {
        if (ex is null)
            return;

        var details = ex.ToString().Replace('\r', ' ').Replace('\n', ' ');
        LogLine(toolKey, $"UNHANDLED | {details}");
    }

    private static string FormatError(ErrorDetail error)
    {
        var sb = new StringBuilder();
        sb.Append("ERROR");

        if (!string.IsNullOrWhiteSpace(error.Code))
            sb.Append(" [").Append(error.Code).Append(']');

        if (!string.IsNullOrWhiteSpace(error.Message))
            sb.Append(' ').Append(error.Message.Trim());

        if (!string.IsNullOrWhiteSpace(error.Details))
            sb.Append(" | ").Append(error.Details.Trim());

        if (error.Exception is not null)
            sb.Append(" | ").Append(error.Exception.GetType().Name).Append(": ").Append(error.Exception.Message);

        return sb.ToString().Replace('\r', ' ').Replace('\n', ' ');
    }

    private static void LogLine(string toolKey, string message)
    {
        var key = Sanitize(toolKey);
        if (string.IsNullOrWhiteSpace(key))
            key = "unknown";

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var logDir = Path.Combine(appData, "DevTools", "logs");
        Directory.CreateDirectory(logDir);

        var filePath = Path.Combine(logDir, $"{key}-{DateTime.Now:yyyyMMdd}.log");
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {message}";

        lock (Sync)
        {
            File.AppendAllText(filePath, line + Environment.NewLine);
        }
    }

    private static string Sanitize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var sb = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            if (char.IsLetterOrDigit(c) || c is '-' or '_')
                sb.Append(char.ToLowerInvariant(c));
        }

        return sb.ToString();
    }
}
