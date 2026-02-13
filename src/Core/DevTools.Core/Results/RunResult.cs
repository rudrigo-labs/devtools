namespace DevTools.Core.Results;

public class RunResult
{
    public bool IsSuccess { get; init; }
    public IReadOnlyList<ErrorDetail> Errors { get; init; } = Array.Empty<ErrorDetail>();
    public RunSummary Summary { get; init; } = RunSummary.Empty;

    public static RunResult Success() => new() { IsSuccess = true };

    public static RunResult Fail(params ErrorDetail[] errors) =>
        new() { IsSuccess = false, Errors = errors ?? Array.Empty<ErrorDetail>() };

    public static RunResult Fail(IEnumerable<ErrorDetail> errors) =>
        new() { IsSuccess = false, Errors = (errors ?? Array.Empty<ErrorDetail>()).ToArray() };
        
    public RunResult WithSummary(RunSummary summary) => new()
    {
        IsSuccess = this.IsSuccess,
        Errors = this.Errors,
        Summary = summary
    };
}
