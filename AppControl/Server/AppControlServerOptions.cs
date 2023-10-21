namespace AppControl.Server;

public class AppControlServerOptions
{
    public AppControlServerTcpOptions TcpOptions { get; set; }
    public bool SecretKeyValidation { get; set; }
    public string SecretKey { get; set; }
    public bool RequireUniqueClientIds { get; set; }
}