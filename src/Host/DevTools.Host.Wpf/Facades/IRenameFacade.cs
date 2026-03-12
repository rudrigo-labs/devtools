using DevTools.Core.Results;
using DevTools.Rename.Models;

namespace DevTools.Host.Wpf.Facades;

public interface IRenameFacade
{
    Task<RunResult<RenameResult>> ExecuteAsync(RenameRequest request, CancellationToken ct = default);
}
