using DevTools.Core.Abstractions;

namespace DevTools.SSHTunnel.Abstractions;

public interface IProcessRunnerWithPid : IProcessRunner
{
    int? LastProcessId { get; }
}
