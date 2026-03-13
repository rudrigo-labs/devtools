using System.Net;
using System.Net.Sockets;
using DevTools.Core.Validation;
using DevTools.SSHTunnel.Models;

namespace DevTools.SSHTunnel.Validators;

public sealed class SshTunnelRequestValidator : IValidator<SshTunnelRequest>
{
    public ValidationResult Validate(SshTunnelRequest instance)
    {
        if (instance is null)
            return ValidationResult.Fail(new ValidationError("request", "Request não pode ser nulo."));

        var errors = new List<ValidationError>();

        if (!Enum.IsDefined(typeof(SshTunnelAction), instance.Action))
        {
            errors.Add(new ValidationError("action", "Action inválida."));
            return ValidationResult.Fail(errors);
        }

        if (instance.Action == SshTunnelAction.Start)
        {
            if (instance.Configuration is null)
            {
                errors.Add(new ValidationError("configuration", "Configuration é obrigatório para Start."));
                return ValidationResult.Fail(errors);
            }

            var p = instance.Configuration;

            if (string.IsNullOrWhiteSpace(p.SshHost))
                errors.Add(new ValidationError("sshHost", "SshHost é obrigatório."));

            if (string.IsNullOrWhiteSpace(p.SshUser))
                errors.Add(new ValidationError("sshUser", "SshUser é obrigatório."));

            if (string.IsNullOrWhiteSpace(p.LocalBindHost))
                errors.Add(new ValidationError("localBindHost", "LocalBindHost é obrigatório."));
            else if (!IsValidBindHost(p.LocalBindHost))
                errors.Add(new ValidationError("localBindHost", "LocalBindHost inválido ou não resolvível."));

            if (string.IsNullOrWhiteSpace(p.RemoteHost))
                errors.Add(new ValidationError("remoteHost", "RemoteHost é obrigatório."));

            if (!IsValidPort(p.SshPort))
                errors.Add(new ValidationError("sshPort", "SshPort deve estar entre 1 e 65535."));

            if (!IsValidPort(p.LocalPort))
                errors.Add(new ValidationError("localPort", "LocalPort deve estar entre 1 e 65535."));

            if (!IsValidPort(p.RemotePort))
                errors.Add(new ValidationError("remotePort", "RemotePort deve estar entre 1 e 65535."));

            if (p.ConnectTimeoutSeconds.HasValue && p.ConnectTimeoutSeconds.Value <= 0)
                errors.Add(new ValidationError("connectTimeoutSeconds", "ConnectTimeoutSeconds deve ser maior que zero."));
        }

        return errors.Count == 0 ? ValidationResult.Success : ValidationResult.Fail(errors);
    }

    private static bool IsValidPort(int port) => port >= 1 && port <= 65535;

    private static bool IsValidBindHost(string host)
    {
        if (IPAddress.TryParse(host, out _)) return true;
        try
        {
            return Dns.GetHostAddresses(host)
                .Any(a => a.AddressFamily is AddressFamily.InterNetwork or AddressFamily.InterNetworkV6);
        }
        catch { return false; }
    }
}

public sealed class SshTunnelEntityValidator : IValidator<SshTunnelEntity>
{
    public ValidationResult Validate(SshTunnelEntity instance)
    {
        if (instance is null)
            return ValidationResult.Fail(new ValidationError("entity", "Entity não pode ser nulo."));

        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(instance.Name))
            errors.Add(new ValidationError("name", "Nome é obrigatório."));

        if (string.IsNullOrWhiteSpace(instance.SshHost))
            errors.Add(new ValidationError("sshHost", "SshHost é obrigatório."));

        if (string.IsNullOrWhiteSpace(instance.SshUser))
            errors.Add(new ValidationError("sshUser", "SshUser é obrigatório."));

        if (instance.SshPort < 1 || instance.SshPort > 65535)
            errors.Add(new ValidationError("sshPort", "SshPort inválido."));

        if (instance.LocalPort < 1 || instance.LocalPort > 65535)
            errors.Add(new ValidationError("localPort", "LocalPort inválido."));

        if (instance.RemotePort < 1 || instance.RemotePort > 65535)
            errors.Add(new ValidationError("remotePort", "RemotePort inválido."));

        return errors.Count == 0 ? ValidationResult.Success : ValidationResult.Fail(errors);
    }
}
