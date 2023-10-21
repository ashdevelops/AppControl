using System.Net;

namespace AppControl.Client;

public class GangClientTcpOptions
{
    public IPAddress Address { get; set; }
    public int Port { get; set; }
}