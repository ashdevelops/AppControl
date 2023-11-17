using System.Buffers;
using System.Net.Sockets;
using System.Text;
using AppControl.Other;
using Newtonsoft.Json;

namespace AppControl.Server;

public class IncomingClient(TcpClient client, Func<ClientConnectedEventArgs, Task>? onClientDisappeared)
{
    public string ClientId { get; set; }
    public DateTime LastPing { get; private set; } = DateTime.Now;
    public ApplicationSession Session { get; set; }
    public bool Validated { get; set; }

    public async Task<AuthPacket?> ReceiveAuthPacketAsync()
    {
        var authPacketBytes = await ReceiveAsync();
        var authPacketString = Encoding.Default.GetString(authPacketBytes);

        try
        {
            return JsonConvert.DeserializeObject<AuthPacket>(authPacketString);
        }
        catch (JsonReaderException e)
        {
            return null;
        }
    }

    public async Task StartListeningAsync()
    {
        while (client.Connected)
        {
            var dataReceived = await ReceiveAsync();
            var message = Encoding.Default.GetString(dataReceived);

            if (message == "PING")
            {
                LastPing = DateTime.Now;
                await WriteAsync(Encoding.Default.GetBytes("PONG"));
            }
        }
    }
    
    public async Task<byte[]> ReceiveAsync()
    {
        var messageLengthBytes = new byte[4];
        var bytesRead = await client.GetStream().ReadAsync(messageLengthBytes, 0, 4);

        if (bytesRead < 4)
        {
            return Array.Empty<byte>();
        }
        
        var messageLength = BitConverter.ToInt32(messageLengthBytes);

        if (messageLength == 0)
        {
            return Array.Empty<byte>();
        }
        
        var buffer = new byte[messageLength];
        await client.GetStream().ReadAsync(buffer);
        
        return buffer;
    }

    public async Task WriteAsync(string s)
    {
        await WriteAsync(Encoding.Default.GetBytes(s));
    }

    private async Task WriteAsync(byte[] bytes)
    {
        bytes = FramingProtocol.WrapMessage(bytes);
        await client.GetStream().WriteAsync(bytes);
    }
    
    public string ExitReason { get; set; }

    public async Task DisposeWithReasonAsync(string reason)
    {
        ExitReason = reason;
        
        var disconnectPacket = new NetworkPacket("DisposedWithReason", new Dictionary<string, object>
        {
            { "reason", reason }
        });
        
        await WriteAsync(JsonConvert.SerializeObject(disconnectPacket));

        if (onClientDisappeared != null)
        {
            await onClientDisappeared.Invoke(new ClientConnectedEventArgs()
            {
                Client = this
            });
        }
        
        client.Client.Shutdown(SocketShutdown.Both);
        client.Dispose();
    }
}