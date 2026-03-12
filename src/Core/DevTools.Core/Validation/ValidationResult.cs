namespace DevTools.Core.Validation;

public sealed class ValidationResult
{
    public static ValidationResult Success { get; } = new(Array.Empty<ValidationError>());

    public IReadOnlyList<ValidationError> Errors { get; }
    public bool IsValid => Errors.Count == 0;

    private ValidationResult(IReadOnlyList<ValidationError> errors)
    {
        Errors = errors;
    }

    public static ValidationResult Fail(params ValidationError[] errors) =>
        new(errors ?? Array.Empty<ValidationError>());

    public static ValidationResult Fail(IEnumerable<ValidationError> errors) =>
        new((errors ?? Array.Empty<ValidationError>()).ToArray());
}

