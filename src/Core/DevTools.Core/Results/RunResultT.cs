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
        Fail(new ErrorDetail(code, message, details, ex));
}
