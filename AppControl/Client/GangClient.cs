using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using AppControl.Server;

namespace AppControl.Client;

public class GangClient : IDisposable
{
    private readonly ILogger<GangClient> _logger;
    private GangClientOptions? _options;
    private readonly GangClientPinger _pinger;
    private readonly TcpClient _client = new();
    private readonly byte[] _buffer = new byte[2048 * 4];

    public GangClient(ILogger<GangClient> logger)
    {
        _logger = logger;
        _pinger = new GangClientPinger(this);
    }

    public async Task StartLongTermSessionAsync(GangClientOptions options)
    {
        _logger.LogInformation("Starting a long term session...");
        
        while (true)
        {
            await ConnectAsync(options);
            await SendAuthPacketAsync();
            await StartPingingAsync();
            await StartListeningAsync();
            
            _logger.LogWarning("Session ended unexpectedly :|");
        }
    }

    private async Task ConnectAsync(GangClientOptions options)
    {
        _logger.LogInformation("Connecting to the server...");
        
        _options = options;
        
        var tcpOptions = _options.TcpOptions;

        try
        {
            await _client.ConnectAsync(new IPEndPoint(tcpOptions.Address, tcpOptions.Port));
        }
        catch (SocketException se)
        {
            if (!_options.RetryOnFailedToConnect)
            {
                throw;
            }
            
            _logger.LogWarning("Failed to connect to server :|");
            
            await Task.Delay(5000);
            await ConnectAsync(options);
        }
    }

    private async Task SendAuthPacketAsync()
    {
        if (_options == null)
        {
            throw new Exception("You must establish options before sending the auth packet.");
        }
        
        var authPacket = new AuthPacket
        {
            ClientId = _options.ClientId,
            SecretKey = _options.SecretKey
        };

        var authPacketString = JsonConvert.SerializeObject(authPacket);
        await WriteAsync(Encoding.Default.GetBytes(authPacketString));
    }

    private async Task StartPingingAsync()
    {
        await _pinger.StartAsync();
    }
    
    public DateTime LastPong { get; set; }

    public async Task SendPingAsync()
    {
        await WriteAsync(Encoding.Default.GetBytes("PING"));
    }

    private async Task WriteAsync(byte[] data)
    {
        if (_disposed)
        {
            return;
        }
        
        await _client.GetStream().WriteAsync(data);
    }
    
    private async Task StartListeningAsync()
    {
        while (_client.Connected)
        {
            var bytes = await _client.Client.ReceiveAsync(_buffer, SocketFlags.None);
                
            if (bytes > 0)
            {
                OnReceived(bytes);
            }
        }
    }

    private void OnReceived(int bytesReceived)
    {
        var data = new byte[bytesReceived];
        Buffer.BlockCopy(_buffer, 0, data, 0, bytesReceived);

        var content = Encoding.Default.GetString(data);

        if (content == "PONG")
        {
            LastPong = DateTime.Now;
            return;
        }

        var args = new GangApplicationMessageReceivedEventArgs
        {
            Content = content
        };
        
        MessageReceivedAsync?.Invoke(args);
    }
    
    public event Func<GangApplicationMessageReceivedEventArgs, Task>? MessageReceivedAsync;
    
    private bool _disposed;
    
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        
        var client = _client?.Client;
            
        client?.Shutdown(SocketShutdown.Both);
        client?.Close();
    }
}