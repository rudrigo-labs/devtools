namespace DevTools.SSHTunnel.Models;

public enum SshStrictHostKeyChecking
{
    Default   = 0,
    Yes       = 1,
    No        = 2,
    AcceptNew = 3
}

public enum SshTunnelAction
{
    Start  = 0,
    Stop   = 1,
    Status = 2
}

public enum TunnelState
{
    Off   = 0,
    On    = 1,
    Error = 2
}
