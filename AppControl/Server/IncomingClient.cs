using System.Buffers;
using System.Net.Sockets;
using System.Text;
using AppControl.Other;
using Newtonsoft.Json;

namespace AppControl.Server;

public class IncomingClient
{
    private readonly TcpClient _client;
    private readonly Func<ClientConnectedEventArgs, Task>? _onClientDisappeared;

    public IncomingClient(TcpClient client, Func<ClientConnectedEventArgs, Task>? onClientDisappeared)
    {
        _client = client;
        _onClientDisappeared = onClientDisappeared;
    }

    public string ClientId { get; set; }
    public DateTime LastPing { get; private set; }
    public ApplicationSession Session { get; set; }

    public async Task<AuthPacket?> ReceiveAuthPacketAsync()
    {
        var authPacketBytes = await ReceiveAsync();
        var authPacketString = Encoding.Default.GetString(authPacketBytes);
        
        return JsonConvert.DeserializeObject<AuthPacket>(authPacketString);
    }

    public async Task StartListeningAsync()
    {
        while (_client.Connected)
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
        var bytesRead = await _client.GetStream().ReadAsync(messageLengthBytes, 0, 4);

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
        await _client.GetStream().ReadAsync(buffer);
        
        return buffer;
    }

    public async Task WriteAsync(string s)
    {
        await WriteAsync(Encoding.Default.GetBytes(s));
    }

    private async Task WriteAsync(byte[] bytes)
    {
        bytes = FramingProtocol.WrapMessage(bytes);
        await _client.GetStream().WriteAsync(bytes);
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

        if (_onClientDisappeared != null)
        {
            await _onClientDisappeared.Invoke(new ClientConnectedEventArgs()
            {
                Client = this
            });
        }
        
        _client.Client.Shutdown(SocketShutdown.Both);
        _client.Dispose();
    }
}