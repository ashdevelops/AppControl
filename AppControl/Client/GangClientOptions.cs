namespace AppControl.Client;

public class GangClientOptions
{
    public string ClientId { get; set; }
    public string SecretKey { get; set; }
    public GangClientTcpOptions TcpOptions { get; set; }
    public bool RetryOnFailedToConnect { get; set; }
}