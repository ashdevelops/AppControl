using System.Net.Sockets;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace AppControl.Server;

public class AppControlServer : IDisposable
{
    private readonly ILogger<AppControlServer> _logger;
    private readonly AppControlServerOptions _serverOptions;
    private readonly AppControlServerTcpOptions _tcpOptions;
    private readonly TcpListener _listener;
    private readonly AppControlServerClientRepository _clientRepository;

    public AppControlServer(
        ILogger<AppControlServer> logger, 
        AppControlServerClientRepository clientRepository, 
        AppControlServerOptions serverOptions)
    {
        _logger = logger;
        _serverOptions = serverOptions;
        _tcpOptions = serverOptions.TcpOptions;
        _listener = new TcpListener(_tcpOptions.IpAddress, _tcpOptions.Port);
        _clientRepository = clientRepository;
        
        _logger.LogWarning($"Loaded version {Assembly.GetAssembly(typeof(AppControlServer))?.GetName().Version?.ToString()} of AppControl package");
    }

    public async Task StartAsync()
    {
        _listener.Start(_tcpOptions.Backlog);
        
        await Task.Factory.StartNew(async () =>
        {
            while (true)
            {
                await _clientRepository.DisconnectIdleClientsAsync(ClientDisappeared);
                await Task.Delay(3000);
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
        
        var incomingClient = new IncomingClient(client, ClientDisappeared);
        var authPacket = await incomingClient.ReceiveAuthPacketAsync();

        if (authPacket == null)
        {
            await incomingClient.DisposeWithReasonAsync("Failed to read auth packet");
            return;
        }
        
        incomingClient.ClientId = authPacket.ClientId;
        
        if (_serverOptions.SecretKeyValidation && 
            !string.IsNullOrEmpty(_serverOptions.SecretKey) &&
            authPacket.SecretKey != _serverOptions.SecretKey)
        {
            await incomingClient.DisposeWithReasonAsync("Failed secret key validation");
            return;
        }

        if (string.IsNullOrEmpty(authPacket.VersionTag))
        {
            await incomingClient.DisposeWithReasonAsync($"Client connected with an empty version tag");
            return;
        }

        if (string.IsNullOrEmpty(authPacket.ClientId))
        {
            await incomingClient.DisposeWithReasonAsync($"Client connected with an empty identifier");
            return;
        }
        
        if (_serverOptions.RequireUniqueClientIds && _clientRepository._clients.ContainsKey(authPacket.ClientId))
        {
            await incomingClient.DisposeWithReasonAsync($"Client {authPacket.ClientId} is already connected");
            return;
        }
        
        if (ValidateClient != null)
        {
            var validatingArgs = new ValidateClientEventArgs
            {
                AuthPacket = authPacket,
                Client = incomingClient
            };

            await ValidateClient.Invoke(validatingArgs);

            if (!string.IsNullOrEmpty(validatingArgs.DenyMessage))
            {
                _logger.LogWarning($"Client denied with: {validatingArgs.DenyMessage}, closing connection...");
                await incomingClient.DisposeWithReasonAsync(validatingArgs.DenyMessage);
                return;
            }
        }

        _clientRepository.AddClient(incomingClient);
        
        _logger.LogWarning($"Client '{authPacket.ClientId}' has connected");

        ClientConnected?.Invoke(new ClientConnectedEventArgs()
        {
            Client = incomingClient
        });
        
        await Task.Factory.StartNew(() => incomingClient.StartListeningAsync(), TaskCreationOptions.LongRunning);
    }

    public event Func<ValidateClientEventArgs, Task>? ValidateClient;
    public event Func<ClientConnectedEventArgs, Task>? ClientConnected;
    public event Func<ClientConnectedEventArgs, Task>? ClientDisappeared;

    public void Dispose()
    {
        _listener.Server.Close();
    }
}