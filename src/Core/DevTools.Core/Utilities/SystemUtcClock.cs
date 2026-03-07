using DevTools.Core.Abstractions;

namespace DevTools.Core.Utilities;

public sealed class SystemUtcClock : IUtcClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}

