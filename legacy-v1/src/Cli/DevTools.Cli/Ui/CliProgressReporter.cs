using DevTools.Core.Abstractions;
using DevTools.Core.Models;

namespace DevTools.Cli.Ui;

public sealed class CliProgressReporter : IProgressReporter, IDisposable
{
    private readonly object _lock = new();
    private readonly CliTheme _theme;
    private int _lastWidth;
    private bool _isFinished;
    private int _spinnerIndex;
    private static readonly char[] Spinner = ['|', '/', '-', '\\'];

    public CliProgressReporter(CliTheme theme)
    {
        _theme = theme;
    }

    public void Report(ProgressEvent ev)
    {
        if (_isFinished)
            return;

        var message = ev.Message ?? string.Empty;
        var (prefix, body) = BuildLine(ev.Percent, message);
        var maxWidth = GetMaxWidth();
        var available = Math.Max(0, maxWidth - prefix.Length);
        if (body.Length > available)
            body = body[..available];

        lock (_lock)
        {
            Console.Write('\r');

            var prev = Console.ForegroundColor;
            try
            {
                Console.ForegroundColor = _theme.AccentColor;
                Console.Write(prefix);

                Console.ForegroundColor = _theme.DimColor;
                Console.Write(body);

                var totalLen = prefix.Length + body.Length;
                if (totalLen < _lastWidth)
                    Console.Write(new string(' ', _lastWidth - totalLen));

                _lastWidth = Math.Max(_lastWidth, totalLen);
            }
            finally
            {
                Console.ForegroundColor = prev;
            }
        }
    }

    public void Finish()
    {
        if (_isFinished)
            return;

        _isFinished = true;
        Console.WriteLine();
    }

    public void Dispose() => Finish();

    private (string Prefix, string Body) BuildLine(int? percent, string message)
    {
        if (!percent.HasValue)
        {
            var spinner = Spinner[_spinnerIndex++ % Spinner.Length];
            return ($"{spinner} ", message);
        }

        var pct = Math.Clamp(percent.Value, 0, 100);
        var width = GetBarWidth();
        var filled = (int)Math.Round(width * (pct / 100.0));
        if (filled < 0) filled = 0;
        if (filled > width) filled = width;

        var bar = new string('#', filled) + new string('-', width - filled);
        return ($"[{bar}] {pct,3}% ", message);
    }

    private static int GetMaxWidth()
    {
        try
        {
            if (!Console.IsOutputRedirected)
                return Math.Max(40, Console.WindowWidth - 1);
        }
        catch
        {
            // ignore
        }

        return 80;
    }

    private static int GetBarWidth()
    {
        var maxWidth = GetMaxWidth();
        var width = maxWidth - 20;
        if (width < 10) width = 10;
        if (width > 36) width = 36;
        return width;
    }
}
