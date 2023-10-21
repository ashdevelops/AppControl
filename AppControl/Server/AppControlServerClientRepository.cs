using Microsoft.Extensions.Logging;

namespace AppControl.Server;

public class AppControlServerClientRepository
{
    private readonly ILogger<AppControlServerClientRepository> _logger;
    public readonly Dictionary<string, IncomingClient> _clients = new();

    public AppControlServerClientRepository(ILogger<AppControlServerClientRepository> logger)
    {
        _logger = logger;
    }
    
    public void AddClient(IncomingClient client)
    {
        _clients[client.ClientId] = client;
    }

    public async Task DisconnectIdleClientsAsync(Func<ClientConnectedEventArgs, Task> onClientDisappeared)
    {
        foreach (var client in _clients.Values)
        {
            var hasPinged = client.LastPing != default;
            var isMissing = (DateTime.Now - client.LastPing).TotalSeconds >= 5;

            if (!hasPinged || !isMissing)
            {
                continue;
            }
            
            _logger.LogWarning($"Disconnecting idle client '{client.ClientId}'");
            _clients.Remove(client.ClientId);
            
            await onClientDisappeared.Invoke(new ClientConnectedEventArgs { Client = client });
            await client.DisposeWithReasonAsync("Marked as idle due to missing ping");
        }
    }
}