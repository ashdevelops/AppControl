using AppControl.Protocol;

namespace AppControl.Server;

public class ValidatingConnectionEventArgs
{
    public AuthPacket AuthPacket { get; set; }
    public ConnectReasonCode ReasonCode { get; set; }
    public string ClientId => AuthPacket.ClientId;
    public string SecretKey => AuthPacket.SecretKey;
    public IncomingClient Client { get; set; }
}