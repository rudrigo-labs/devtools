namespace DevTools.Core.Results;

public sealed class RunResult<T> : RunResult
{
    public T? Value { get; init; }

    public static RunResult<T> Success(T value) =>
        new() { IsSuccess = true, Value = value };

    public static new RunResult<T> Fail(params ErrorDetail[] errors) =>
        new() { IsSuccess = false, Errors = errors ?? Array.Empty<ErrorDetail>() };

    public static new RunResult<T> Fail(IEnumerable<ErrorDetail> errors) =>
        new() { IsSuccess = false, Errors = (errors ?? Array.Empty<ErrorDetail>()).ToArray() };

    public static RunResult<T> FromException(string code, string message, Exception ex, string? details = null) =>
        Fail(new ErrorDetail(code, message, Details: details, Exception: ex));
        
    public new RunResult<T> WithSummary(RunSummary summary) => new()
    {
        IsSuccess = this.IsSuccess,
        Errors = this.Errors,
        Summary = summary,
        Value = this.Value
    };
}
