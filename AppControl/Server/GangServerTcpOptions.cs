using System.Net;

namespace AppControl.Server;

public class GangServerTcpOptions
{
    public IPAddress IpAddress { get; set; }
    public int Port { get; set; }
    public int Backlog { get; set; }
}