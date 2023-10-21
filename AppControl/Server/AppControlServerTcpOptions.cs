using System.Net;

namespace AppControl.Server;

public class AppControlServerTcpOptions
{
    public IPAddress IpAddress { get; set; }
    public int Port { get; set; }
    public int Backlog { get; set; }
}