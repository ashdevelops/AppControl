using AppControl.Protocol;

namespace AppControl.Server;

public class ValidateClientEventArgs
{
    public AuthPacket AuthPacket { get; set; }
    public DenyConnectReasonCode ReasonCode { get; set; }
    public string ClientId => AuthPacket.ClientId;
    public string SecretKey => AuthPacket.SecretKey;
    public IncomingClient Client { get; set; }
}