namespace AppControl.Client;

public class AppControlClientOptions
{
    public string ClientId { get; set; }
    public string SecretKey { get; set; }
    public AppControlClientTcpOptions TcpOptions { get; set; }
    public bool RetryOnFailedToConnect { get; set; }
}