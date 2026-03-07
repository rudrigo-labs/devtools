namespace DevTools.Core.Abstractions;

public interface IUtcClock
{
    DateTime UtcNow { get; }
}

