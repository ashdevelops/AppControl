using System.Net;
using Microsoft.Extensions.Configuration;

namespace AppControl.Client;

public class AppControlClientOptionsBuilder
{
    private readonly AppControlClientOptions _options = new();
    
    public AppControlClientOptionsBuilder WithClientId(string clientId)
    {
        _options.ClientId = clientId;
        return this;
    }

    public AppControlClientOptionsBuilder WithSecretKey(string secretKey)
    {
        _options.SecretKey = secretKey;
        return this;
    }

    public AppControlClientOptionsBuilder RetryOnFailedToConnect()
    {
        _options.RetryOnFailedToConnect = true;
        return this;
    }

    public AppControlClientOptionsBuilder WithTcpOptions(IPAddress ipAddress, int port)
    {
        _options.TcpOptions = new AppControlClientTcpOptions
        {
            Address = ipAddress,
            Port = port
        };

        return this;
    }
    
    public AppControlClientOptionsBuilder CreateDefaultBuilder(IConfiguration configuration)
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
    
    public AppControlClientOptions Build()
    {
        return _options;
    }
}