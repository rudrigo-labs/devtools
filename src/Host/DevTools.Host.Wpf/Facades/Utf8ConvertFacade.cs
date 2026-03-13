using DevTools.Core.Results;
using DevTools.Utf8Convert.Engine;
using DevTools.Utf8Convert.Models;

namespace DevTools.Host.Wpf.Facades;

public interface IUtf8ConvertFacade
{
    Task<RunResult<Utf8ConvertResult>> ExecuteAsync(Utf8ConvertRequest request, CancellationToken ct = default);
}

public sealed class Utf8ConvertFacade : IUtf8ConvertFacade
{
    private readonly Utf8ConvertEngine _engine;

    public Utf8ConvertFacade(Utf8ConvertEngine engine)
    {
        _engine = engine;
    }

    public Task<RunResult<Utf8ConvertResult>> ExecuteAsync(Utf8ConvertRequest request, CancellationToken ct = default) =>
        _engine.ExecuteAsync(request, cancellationToken: ct);
}
