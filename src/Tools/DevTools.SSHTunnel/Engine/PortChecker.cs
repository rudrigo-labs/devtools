using System.Net;
using System.Net.Sockets;

namespace DevTools.SSHTunnel.Engine;

public static class PortChecker
{
    public static bool IsPortFree(string bindHost, int port)
    {
        if (!TryResolveBindHost(bindHost, out var addresses))
            return false;

        foreach (var address in addresses)
        {
            if (!IsPortFree(address, port))
                return false;
        }

        return true;
    }

    public static bool TryResolveBindHost(string bindHost, out IReadOnlyList<IPAddress> addresses)
    {
        if (IPAddress.TryParse(bindHost, out var ip))
        {
            addresses = new[] { ip };
            return true;
        }

        try
        {
            var resolved = Dns.GetHostAddresses(bindHost)
                .Where(a => a.AddressFamily is AddressFamily.InterNetwork or AddressFamily.InterNetworkV6)
                .ToArray();

            if (resolved.Length == 0)
            {
                addresses = Array.Empty<IPAddress>();
                return false;
            }

            addresses = resolved;
            return true;
        }
        catch
        {
            addresses = Array.Empty<IPAddress>();
            return false;
        }
    }

    private static bool IsPortFree(IPAddress address, int port)
    {
        TcpListener? listener = null;
        try
        {
            listener = new TcpListener(address, port);
            listener.Start();
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
        catch
        {
            return false;
        }
        finally
        {
            try { listener?.Stop(); } catch { }
        }
    }
}
