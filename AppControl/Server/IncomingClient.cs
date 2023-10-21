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
            var message = Encoding.Default.GetString(await ReceiveAsync());

            if (message == "PING")
            {
                LastPing = DateTime.Now;
                await WriteAsync(Encoding.Default.GetBytes("PONG"));
            }
        }
    }

    public async Task<byte[]> ReceiveAsync()
    {
        var bytes = await _client.Client.ReceiveAsync(_buffer, SocketFlags.None);
        var data = new byte[bytes];
        Buffer.BlockCopy(_buffer, 0, data, 0, bytes);

        return data;
    }

    public async Task WriteAsync(byte[] bytes)
    {
        await _client.GetStream().WriteAsync(bytes);
    }

    public async Task DisposeWithReasonAsync(string reason)
    {
        await WriteAsync(Encoding.Default.GetBytes(reason));
        
        _client.Client.Shutdown(SocketShutdown.Both);
        _client.Dispose();
    }
}