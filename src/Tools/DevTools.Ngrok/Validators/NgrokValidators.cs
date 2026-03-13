using DevTools.Core.Validation;
using DevTools.Ngrok.Models;

namespace DevTools.Ngrok.Validators;

public sealed class NgrokRequestValidator : IValidator<NgrokRequest>
{
    public ValidationResult Validate(NgrokRequest instance)
    {
        if (instance is null)
            return ValidationResult.Fail(new ValidationError("request", "Request não pode ser nulo."));

        var errors = new List<ValidationError>();

        if (!Enum.IsDefined(typeof(NgrokAction), instance.Action))
        {
            errors.Add(new ValidationError("action", "Action inválida."));
            return ValidationResult.Fail(errors);
        }

        if (instance.TimeoutSeconds <= 0)
            errors.Add(new ValidationError("timeoutSeconds", "TimeoutSeconds deve ser maior que zero."));

        if (instance.RetryCount < 0)
            errors.Add(new ValidationError("retryCount", "RetryCount deve ser >= 0."));

        if (RequiresApi(instance.Action))
        {
            var baseUrl = string.IsNullOrWhiteSpace(instance.BaseUrl) ? "http://127.0.0.1:4040/" : instance.BaseUrl.Trim();
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out _))
                errors.Add(new ValidationError("baseUrl", "BaseUrl deve ser uma URL absoluta válida."));
        }

        if (instance.Action == NgrokAction.CloseTunnel && string.IsNullOrWhiteSpace(instance.TunnelName))
            errors.Add(new ValidationError("tunnelName", "TunnelName é obrigatório para CloseTunnel."));

        if (instance.Action == NgrokAction.StartHttp)
        {
            if (instance.StartOptions is null)
            {
                errors.Add(new ValidationError("startOptions", "StartOptions é obrigatório para StartHttp."));
            }
            else
            {
                if (instance.StartOptions.Port < 1 || instance.StartOptions.Port > 65535)
                    errors.Add(new ValidationError("port", "Port deve estar entre 1 e 65535."));

                if (!IsValidProtocol(instance.StartOptions.Protocol))
                    errors.Add(new ValidationError("protocol", "Protocol deve ser 'http' ou 'https'."));
            }
        }

        return errors.Count == 0 ? ValidationResult.Success : ValidationResult.Fail(errors);
    }

    private static bool RequiresApi(NgrokAction action) =>
        action is NgrokAction.ListTunnels or NgrokAction.CloseTunnel;

    private static bool IsValidProtocol(string? proto) =>
        string.IsNullOrWhiteSpace(proto) ||
        proto.Equals("http", StringComparison.OrdinalIgnoreCase) ||
        proto.Equals("https", StringComparison.OrdinalIgnoreCase);
}

public sealed class NgrokEntityValidator : IValidator<NgrokEntity>
{
    public ValidationResult Validate(NgrokEntity instance)
    {
        if (instance is null)
            return ValidationResult.Fail(new ValidationError("entity", "Entity não pode ser nulo."));

        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(instance.Name))
            errors.Add(new ValidationError("name", "Nome é obrigatório."));

        return errors.Count == 0 ? ValidationResult.Success : ValidationResult.Fail(errors);
    }
}
