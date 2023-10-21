using System.Net;

namespace AppControl.Client;

public class AppControlClientTcpOptions
{
    public IPAddress Address { get; set; }
    public int Port { get; set; }
}