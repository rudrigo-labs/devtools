using DevTools.Core.Models;

namespace DevTools.Core.Abstractions;

public interface IProgressReporter
{
    void Report(ProgressEvent ev);
}
