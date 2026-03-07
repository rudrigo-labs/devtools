namespace DevTools.Core.Models;

public sealed record ProgressEvent(
    string Message,
    int? Percent = null,
    string? Scope = null,
    DateTimeOffset? Timestamp = null)
{
    public DateTimeOffset TimestampValue => Timestamp ?? DateTimeOffset.UtcNow;
}
