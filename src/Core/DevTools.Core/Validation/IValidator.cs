namespace DevTools.Core.Validation;

public interface IValidator<in T>
{
    ValidationResult Validate(T instance);
}

