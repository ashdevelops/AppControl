namespace AppControl.Other;

public class AuthPacket
{
    public string ClientId { get; set; }
    public string SecretKey { get; set; }
    public string HostName { get; set; }
    public string UserName { get; set; }
    public string IpAddress { get; set; }
    public string VersionTag { get; set; }
    public DateTime LaunchedAt { get; set; }
}