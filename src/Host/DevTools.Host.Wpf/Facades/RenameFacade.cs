using DevTools.Core.Results;
using DevTools.Rename.Engine;
using DevTools.Rename.Models;

namespace DevTools.Host.Wpf.Facades;

public sealed class RenameFacade : IRenameFacade
{
    private readonly RenameEngine _engine;

    public RenameFacade(RenameEngine engine)
    {
        _engine = engine;
    }

    public Task<RunResult<RenameResult>> ExecuteAsync(RenameRequest request, CancellationToken ct = default) =>
        _engine.ExecuteAsync(request, ct: ct);
}
