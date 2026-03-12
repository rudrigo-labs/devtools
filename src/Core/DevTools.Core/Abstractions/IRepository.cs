using DevTools.Core.Models;

namespace DevTools.Core.Abstractions;

public interface IRepository<T> where T : NamedConfiguration
{
    Task<IReadOnlyList<T>> ListAsync(CancellationToken ct = default);
    Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task UpsertAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}

