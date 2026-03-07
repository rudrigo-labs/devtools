using DevTools.Core.Models;

namespace DevTools.Core.Abstractions;

public interface IProcessRunner
{
    Task<ProcessResult> RunAsync(
        string fileName,
        string arguments,
        string? workingDirectory = null,
        IDictionary<string, string?>? environment = null,
        CancellationToken ct = default);
}
