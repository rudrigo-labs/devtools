namespace DevTools.Core.Models;

public sealed record ProcessResult(
    int ExitCode,
    string StdOut,
    string StdErr,
    TimeSpan Duration);
