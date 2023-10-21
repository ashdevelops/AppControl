using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using AppControl.Protocol;

namespace AppControl.Server;

public class AppControlServer : IDisposable
{
    private readonly ILogger<AppControlServer> _logger;
    private readonly AppControlServerOptions _serverOptions;
    private readonly AppControlServerTcpOptions _tcpOptions;
    private readonly TcpListener _listener;
    private readonly AppControlServerClientRepository _clientRepository;

    public AppControlServer(ILogger<AppControlServer> logger, 
        AppControlServerClientRepository clientRepository, 
        AppControlServerOptions serverOptions)
    {
        _logger = logger;
        _serverOptions = serverOptions;
        _tcpOptions = serverOptions.TcpOptions;
        _listener = new TcpListener(_tcpOptions.IpAddress, _tcpOptions.Port);
        _clientRepository = clientRepository;
    }

    public async Task StartAsync()
    {
        _listener.Start(_tcpOptions.Backlog);
        
        await Task.Factory.StartNew(async () =>
        {
            while (true)
            {
                await _clientRepository.DisconnectIdleClientsAsync();
                await Task.Delay(10000);
            }
        }, TaskCreationOptions.LongRunning);
    }

    public async Task ListenAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Listening for connections on port {_tcpOptions.Port}");
        
        while (!cancellationToken.IsCancellationRequested)
        {
            var client = await _listener.AcceptTcpClientAsync(cancellationToken);
            await AcceptClientAsync(client);
        }
    }
    
    private async Task AcceptClientAsync(TcpClient client)
    {
        _logger.LogInformation("A client is trying to connect...");
        
        var incomingClient = new IncomingClient(client);
        var authPacket = await incomingClient.ReceiveAuthPacketAsync();

        if (authPacket == null)
        {
            await incomingClient.DisposeWithReasonAsync("Failed to read auth packet");
            return;
        }

        if (_serverOptions.SecretKeyValidation && 
            !string.IsNullOrEmpty(_serverOptions.SecretKey) &&
            authPacket.SecretKey != _serverOptions.SecretKey)
        {
            await incomingClient.DisposeWithReasonAsync("Failed secret key validation");
            return;
        }
        
        if (_serverOptions.RequireUniqueClientIds && _clientRepository._clients.ContainsKey(authPacket.ClientId))
        {
            await incomingClient.DisposeWithReasonAsync($"Client {authPacket.ClientId} is already connected");
            return;
        }
        
        var validatingArgs = new ValidatingConnectionEventArgs
        {
            AuthPacket = authPacket,
            Client = incomingClient
        };

        ValidatingConnectionAsync?.Invoke(validatingArgs);

        if (validatingArgs.ReasonCode != ConnectReasonCode.Success)
        {
            _logger.LogWarning($"Failed with: {validatingArgs.ReasonCode.ToString()}, closing connection...");
            await incomingClient.DisposeWithReasonAsync(validatingArgs.ReasonCode.ToString());
            return;
        }

        incomingClient.ClientId = authPacket.ClientId;

        _clientRepository.AddClient(incomingClient);
        
        _logger.LogWarning($"Client {authPacket.ClientId} has connected");

        await Task.Factory.StartNew(() => incomingClient.StartListeningAsync(), TaskCreationOptions.LongRunning);
    }

    public event Func<ValidatingConnectionEventArgs, Task> ValidatingConnectionAsync;

    public void Dispose()
    {
        _listener.Server.Close();
    }
}