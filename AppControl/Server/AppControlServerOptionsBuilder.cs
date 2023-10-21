using System.Net;
using Microsoft.Extensions.Configuration;

namespace AppControl.Server;

public class AppControlServerOptionsBuilder
{
    private readonly AppControlServerOptions _options = new();

    public AppControlServerOptionsBuilder WithTcpOptions(IPAddress ipAddress, int port, int backlog = 500)
    {
        _options.TcpOptions = new AppControlServerTcpOptions()
        {
            IpAddress = ipAddress,
            Port = port,
            Backlog = backlog
        };
        
        return this;
    }

    public AppControlServerOptionsBuilder WithSecretKeyValidation(string secretKey)
    {
        _options.SecretKeyValidation = true;
        _options.SecretKey = secretKey;
        
        return this;
    }

    public AppControlServerOptionsBuilder RequireUniqueClientIds()
    {
        _options.RequireUniqueClientIds = true;
        return this;
    }

    public AppControlServerOptionsBuilder CreateDefaultBuilder(IConfiguration configuration)
    {
        var secretKey = configuration.GetValue<string>("Networking:Validation:SecretKey");
        var host = configuration.GetValue<string>("Networking:Host");
        var port = configuration.GetValue<int>("Networking:Port");
        
        if (secretKey == null || host == null || port == 0)
        {
            throw new Exception("Failed to build default builder due to missing config items");
        }
        
        return WithSecretKeyValidation(secretKey)
            .RequireUniqueClientIds()
            .WithTcpOptions(IPAddress.Parse(host), port);
    }
    
    public AppControlServerOptions Build()
    {
        return _options;
    }
}