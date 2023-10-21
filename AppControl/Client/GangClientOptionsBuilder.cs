using System.Net;
using Microsoft.Extensions.Configuration;

namespace AppControl.Client;

public class GangClientOptionsBuilder
{
    private readonly GangClientOptions _options = new();
    
    public GangClientOptionsBuilder WithClientId(string clientId)
    {
        _options.ClientId = clientId;
        return this;
    }

    public GangClientOptionsBuilder WithSecretKey(string secretKey)
    {
        _options.SecretKey = secretKey;
        return this;
    }

    public GangClientOptionsBuilder RetryOnFailedToConnect()
    {
        _options.RetryOnFailedToConnect = true;
        return this;
    }

    public GangClientOptionsBuilder WithTcpOptions(IPAddress ipAddress, int port)
    {
        _options.TcpOptions = new GangClientTcpOptions
        {
            Address = ipAddress,
            Port = port
        };

        return this;
    }
    
    public GangClientOptionsBuilder CreateDefaultBuilder(IConfiguration configuration)
    {
        var clientId = configuration.GetValue<string>("Networking:ClientId");
        var secretKey = configuration.GetValue<string>("Networking:Validation:SecretKey");
        var host = configuration.GetValue<string>("Networking:Host");
        var port = configuration.GetValue<int>("Networking:Port");
        
        if (clientId == null || secretKey == null || host == null || port == 0)
        {
            throw new Exception("Failed to build default builder due to missing config items");
        }

        return WithClientId(clientId)
            .WithSecretKey(secretKey)
            .WithTcpOptions(IPAddress.Parse(host), port)
            .RetryOnFailedToConnect();
    }
    
    public GangClientOptions Build()
    {
        return _options;
    }
}