namespace DevTools.Core.Results;

public sealed record ErrorDetail(
    string Code,
    string Message,
    string? Cause = null,
    string? Action = null,
    string? Details = null,
    Exception? Exception = null);
