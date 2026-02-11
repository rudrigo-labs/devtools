namespace DevTools.SSHTunnel.Engine;

public abstract class SshTunnelException : Exception
{
    protected SshTunnelException(string code, string message, string? details = null, Exception? inner = null)
        : base(message, inner)
    {
        Code = code;
        Details = details;
    }

    public string Code { get; }
    public string? Details { get; }
}

public sealed class SshTunnelConfigException : SshTunnelException
{
    public SshTunnelConfigException(string code, string message, string? details = null, Exception? inner = null)
        : base(code, message, details, inner)
    {
    }
}

public sealed class SshTunnelConnectionException : SshTunnelException
{
    public SshTunnelConnectionException(string code, string message, string? details = null, Exception? inner = null)
        : base(code, message, details, inner)
    {
    }
}
