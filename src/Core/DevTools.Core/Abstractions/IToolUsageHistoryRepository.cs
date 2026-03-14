using DevTools.Core.Models;

namespace DevTools.Core.Abstractions;

public interface IToolUsageHistoryRepository
{
    Task<IReadOnlyList<ToolUsageHistoryEntry>> ListAsync(string toolSlug, CancellationToken ct = default);
    Task AddAsync(string toolSlug, ToolUsageHistoryEntry entry, int maxItems = 10, CancellationToken ct = default);
}
