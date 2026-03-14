namespace DevTools.Core.Models;

public sealed class ToolUsageHistoryEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime UsedAtUtc { get; set; } = DateTime.UtcNow;
    public string Title { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
}
