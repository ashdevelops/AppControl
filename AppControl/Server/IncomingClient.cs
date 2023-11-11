using System.Buffers;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;

namespace AppControl.Server;

public class IncomingClient
{
    private readonly TcpClient _client;
    private readonly byte[] _buffer;

    public IncomingClient(TcpClient client)
    {
        _client = client;
        _buffer = new byte[4096];
    }

    public string ClientId { get; set; }
    public DateTime LastPing { get; private set; }

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
        var messageBytesRead = await _client.GetStream().ReadAsync(buffer);
        
        return buffer;
    }

    public async Task WriteAsync(byte[] bytes)
    {
        bytes = FramingProtocol.WrapMessage(bytes);
        await _client.GetStream().WriteAsync(bytes);
    }

    public async Task DisposeWithReasonAsync(string reason)
    {
        await WriteAsync(Encoding.Default.GetBytes(reason));
        
        _client.Client.Shutdown(SocketShutdown.Both);
        _client.Dispose();
    }
}