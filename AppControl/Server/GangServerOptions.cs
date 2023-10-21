namespace AppControl.Server;

public class GangServerOptions
{
    public GangServerTcpOptions TcpOptions { get; set; }
    public bool SecretKeyValidation { get; set; }
    public string SecretKey { get; set; }
    public bool RequireUniqueClientIds { get; set; }
}