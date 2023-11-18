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

    public async Task DisconnectIdleClientsAsync(Func<ClientConnectedEventArgs, Task>? onClientDisappeared)
    {
        _logger.LogInformation("Checking for idle clients...");
        
        foreach (var client in _clients.Values)
        {
            var isMissing = (DateTime.Now - client.LastPing).TotalSeconds >= 5;

            if (!isMissing)
            {
                continue;
            }
            
            _logger.LogWarning($"Disconnecting idle client '{client.ClientId}'");
            _clients.Remove(client.ClientId);
            
            await client.DisposeWithReasonAsync("Marked as idle due to missing ping");
            
            if (onClientDisappeared != null)
            {
                await onClientDisappeared.Invoke(new ClientConnectedEventArgs { Client = client });
            }
        }
    }
}