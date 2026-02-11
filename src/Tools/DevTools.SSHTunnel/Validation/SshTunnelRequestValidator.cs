using System.Net;
using System.Net.Sockets;
using DevTools.Core.Results;
using DevTools.SSHTunnel.Models;

namespace DevTools.SSHTunnel.Validation;

public static class SshTunnelRequestValidator
{
    public static IReadOnlyList<ErrorDetail> Validate(SshTunnelRequest request)
    {
        var errors = new List<ErrorDetail>();

        if (request is null)
        {
            errors.Add(new ErrorDetail("sshtunnel.request.null", "Request is null."));
            return errors;
        }

        if (!Enum.IsDefined(typeof(SshTunnelAction), request.Action))
        {
            errors.Add(new ErrorDetail("sshtunnel.action.invalid", "Action is invalid."));
            return errors;
        }

        if (request.Action == SshTunnelAction.Start)
        {
            if (request.Profile is null)
            {
                errors.Add(new ErrorDetail("sshtunnel.profile.required", "Profile is required for Start."));
                return errors;
            }

            var p = request.Profile;

            if (string.IsNullOrWhiteSpace(p.SshHost))
                errors.Add(new ErrorDetail("sshtunnel.sshhost.required", "SshHost is required."));

            if (string.IsNullOrWhiteSpace(p.SshUser))
                errors.Add(new ErrorDetail("sshtunnel.sshuser.required", "SshUser is required."));

            if (string.IsNullOrWhiteSpace(p.LocalBindHost))
                errors.Add(new ErrorDetail("sshtunnel.localbind.required", "LocalBindHost is required."));
            else if (!IsValidBindHost(p.LocalBindHost))
                errors.Add(new ErrorDetail("sshtunnel.localbind.invalid", "LocalBindHost is invalid or not resolvable."));

            if (string.IsNullOrWhiteSpace(p.RemoteHost))
                errors.Add(new ErrorDetail("sshtunnel.remotehost.required", "RemoteHost is required."));

            if (!IsValidPort(p.SshPort))
                errors.Add(new ErrorDetail("sshtunnel.sshport.invalid", "SshPort must be between 1 and 65535."));

            if (!IsValidPort(p.LocalPort))
                errors.Add(new ErrorDetail("sshtunnel.localport.invalid", "LocalPort must be between 1 and 65535."));

            if (!IsValidPort(p.RemotePort))
                errors.Add(new ErrorDetail("sshtunnel.remoteport.invalid", "RemotePort must be between 1 and 65535."));

            if (p.ConnectTimeoutSeconds.HasValue && p.ConnectTimeoutSeconds.Value <= 0)
                errors.Add(new ErrorDetail("sshtunnel.timeout.invalid", "ConnectTimeoutSeconds must be greater than zero."));

            if (!Enum.IsDefined(typeof(SshStrictHostKeyChecking), p.StrictHostKeyChecking))
                errors.Add(new ErrorDetail("sshtunnel.strict_host_key.invalid", "StrictHostKeyChecking is invalid."));
        }

        return errors;
    }

    private static bool IsValidPort(int port)
        => port >= 1 && port <= 65535;

    private static bool IsValidBindHost(string host)
    {
        if (IPAddress.TryParse(host, out _))
            return true;

        try
        {
            var resolved = Dns.GetHostAddresses(host)
                .Any(a => a.AddressFamily is AddressFamily.InterNetwork or AddressFamily.InterNetworkV6);
            return resolved;
        }
        catch
        {
            return false;
        }
    }
}
