namespace DevTools.Core.Results;

public sealed record ErrorDetail(
    string Code,
    string Message,
    string? Details = null,
    Exception? Exception = null);
