using System.Net;

namespace DevTools.Ngrok.Engine;

public sealed class NgrokApiException : Exception
{
    public NgrokApiException(string code, string message, string? details = null, HttpStatusCode? statusCode = null, Exception? inner = null)
        : base(message, inner)
    {
        Code = code;
        Details = details;
        StatusCode = statusCode;
    }

    public string Code { get; }
    public string? Details { get; }
    public HttpStatusCode? StatusCode { get; }
}
