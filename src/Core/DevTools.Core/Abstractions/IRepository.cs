using DevTools.Core.Contracts;

namespace DevTools.Core.Abstractions;

public interface IRepository<T> where T : ToolEntityBase
{
    Task<IReadOnlyList<T>> ListAsync(CancellationToken ct = default);
    Task<T?> GetByIdAsync(string id, CancellationToken ct = default);
    Task UpsertAsync(T entity, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}

