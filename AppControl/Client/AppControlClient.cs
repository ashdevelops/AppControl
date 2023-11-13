using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using AppControl.Other;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using AppControl.Server;
using SharedSupport;

namespace AppControl.Client;

public class AppControlClient : IDisposable
{
    private readonly ILogger<AppControlClient> _logger;
    private AppControlClientOptions? _options;
    private readonly AppControlClientPinger _pinger;
    private readonly TcpClient _client = new();

    public AppControlClient(ILogger<AppControlClient> logger)
    {
        _logger = logger;
        _pinger = new AppControlClientPinger(this);
        
        _logger.LogWarning($"Loaded version {Assembly.GetAssembly(typeof(AppControlServer))?.GetName().Version?.ToString()} of AppControl package");
    }

    public async Task StartLongTermSessionAsync(AppControlClientOptions options)
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
    
    private async Task ConnectAsync(AppControlClientOptions options)
    {
        _logger.LogInformation("Attempting to connect to the AppServer...");
        
        _options = options;
        
        var tcpOptions = _options.TcpOptions;

        try
        {
            await _client.ConnectAsync(new IPEndPoint(tcpOptions.Address, tcpOptions.Port));
            _logger.LogInformation("We are now connected to the AppServer!");
        }
        catch (Exception e)
        {
            if (!_options.RetryOnFailedToConnect || e is not (SocketException or IOException))
            {
                throw;
            }
            
            _logger.LogWarning("Couldn't connect to the AppServer, waiting and retrying...");
            
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
            SecretKey = _options.SecretKey,
            HostName = Environment.MachineName,
            UserName = Environment.UserName,
            IpAddress = await HttpUtilities.DownloadAsync("https://ip.paz.bio")
        };

        var authPacketString = JsonConvert.SerializeObject(authPacket);
        var authPacketBytes = Encoding.Default.GetBytes(authPacketString);
        
        await WriteAsync(authPacketBytes);
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

        data = FramingProtocol.WrapMessage(data);
        await _client.GetStream().WriteAsync(data);
    }
    
    private async Task StartListeningAsync()
    {
        while (_client.Connected)
        {
            var data = await ReceiveAsync();

            if (data.Length < 1)
            {
                return;
            }
            
            OnReceived(data);
        }
    }

    private async Task<byte[]> ReceiveAsync()
    {
        var lengthPrefixBytes = new byte[sizeof(int)];
        
        try
        {
            await _client.GetStream().ReadExactlyAsync(lengthPrefixBytes);
        }
        catch (EndOfStreamException)
        {
            return Array.Empty<byte>();
        }
        
        var messageLength = BitConverter.ToInt32(lengthPrefixBytes);

        if (messageLength == 0)
        {
            return Array.Empty<byte>();
        }

        var buffer = new byte[messageLength];
        await _client.GetStream().ReadExactlyAsync(buffer);

        return buffer;
    }

    private void OnReceived(byte[] data)
    {
        var content = Encoding.Default.GetString(data);

        if (content == "PONG")
        {
            LastPong = DateTime.Now;
            return;
        }

        var args = new AppControlApplicationMessageReceivedEventArgs
        {
            Content = content
        };

        var packet = JsonConvert.DeserializeObject<NetworkPacket>(content);
        
        if (packet is { Name: "FailedAuth" } && packet.Data.TryGetValue("reason", out var reason))
        {
            OnAuthFailed?.Invoke(new AppControlApplicationAuthFailedEventArgs
            {
                Reason = reason.ToString() ?? string.Empty
            });
            
            return;
        }
        
        MessageReceivedAsync?.Invoke(args);
    }
    
    public event Func<AppControlApplicationMessageReceivedEventArgs, Task>? MessageReceivedAsync;
    public event Func<AppControlApplicationAuthFailedEventArgs, Task>? OnAuthFailed;
    
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