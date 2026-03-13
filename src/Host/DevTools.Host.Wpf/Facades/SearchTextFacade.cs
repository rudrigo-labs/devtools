using DevTools.Core.Results;
using DevTools.SearchText.Engine;
using DevTools.SearchText.Models;

namespace DevTools.Host.Wpf.Facades;

public interface ISearchTextFacade
{
    Task<RunResult<SearchTextResult>> ExecuteAsync(SearchTextRequest request, CancellationToken ct = default);
}

public sealed class SearchTextFacade : ISearchTextFacade
{
    private readonly SearchTextEngine _engine;

    public SearchTextFacade(SearchTextEngine engine)
    {
        _engine = engine;
    }

    public Task<RunResult<SearchTextResult>> ExecuteAsync(SearchTextRequest request, CancellationToken ct = default) =>
        _engine.ExecuteAsync(request, cancellationToken: ct);
}
