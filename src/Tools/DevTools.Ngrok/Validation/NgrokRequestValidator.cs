using DevTools.Core.Results;
using DevTools.Ngrok.Models;

namespace DevTools.Ngrok.Validation;

public static class NgrokRequestValidator
{
    public static IReadOnlyList<ErrorDetail> Validate(NgrokRequest request)
    {
        var errors = new List<ErrorDetail>();

        if (request is null)
        {
            errors.Add(new ErrorDetail("ngrok.request.null", "Request is null."));
            return errors;
        }

        if (!Enum.IsDefined(typeof(NgrokAction), request.Action))
        {
            errors.Add(new ErrorDetail("ngrok.action.invalid", "Action is invalid."));
            return errors;
        }

        if (request.TimeoutSeconds <= 0)
            errors.Add(new ErrorDetail("ngrok.timeout.invalid", "TimeoutSeconds must be greater than zero."));

        if (request.RetryCount < 0)
            errors.Add(new ErrorDetail("ngrok.retry.invalid", "RetryCount must be >= 0."));

        if (RequiresApi(request.Action))
        {
            var baseUrl = string.IsNullOrWhiteSpace(request.BaseUrl)
                ? "http://127.0.0.1:4040/"
                : request.BaseUrl.Trim();

            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out _))
                errors.Add(new ErrorDetail("ngrok.base_url.invalid", "BaseUrl must be a valid absolute URL."));
        }

        if (request.Action == NgrokAction.CloseTunnel && string.IsNullOrWhiteSpace(request.TunnelName))
            errors.Add(new ErrorDetail("ngrok.tunnel.required", "TunnelName is required for CloseTunnel."));

        if (request.Action == NgrokAction.StartHttp)
        {
            if (request.StartOptions is null)
            {
                errors.Add(new ErrorDetail("ngrok.start.options.required", "StartOptions is required for StartHttp."));
            }
            else
            {
                if (!IsValidPort(request.StartOptions.Port))
                    errors.Add(new ErrorDetail("ngrok.start.port.invalid", "Port must be between 1 and 65535."));

                if (!IsValidProtocol(request.StartOptions.Protocol))
                    errors.Add(new ErrorDetail("ngrok.start.protocol.invalid", "Protocol must be 'http' or 'https'."));
            }
        }

        return errors;
    }

    private static bool RequiresApi(NgrokAction action)
        => action is NgrokAction.ListTunnels or NgrokAction.CloseTunnel;

    private static bool IsValidPort(int port)
        => port >= 1 && port <= 65535;

    private static bool IsValidProtocol(string? proto)
    {
        if (string.IsNullOrWhiteSpace(proto))
            return true;

        return proto.Equals("http", StringComparison.OrdinalIgnoreCase)
               || proto.Equals("https", StringComparison.OrdinalIgnoreCase);
    }
}
