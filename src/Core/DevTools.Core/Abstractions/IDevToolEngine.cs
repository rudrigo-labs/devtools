using System.Threading;
using System.Threading.Tasks;
using DevTools.Core.Results;

namespace DevTools.Core.Abstractions;

public interface IDevToolEngine<in TRequest, TResponse>
{
    Task<RunResult<TResponse>> ExecuteAsync(
        TRequest request,
        IProgressReporter? progress = null,
        CancellationToken ct = default);
}
