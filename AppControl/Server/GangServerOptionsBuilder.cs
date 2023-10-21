using System.Net;
using Microsoft.Extensions.Configuration;

namespace AppControl.Server;

public class GangServerOptionsBuilder
{
    private readonly GangServerOptions _options = new();

    public GangServerOptionsBuilder WithTcpOptions(IPAddress ipAddress, int port, int backlog = 500)
    {
        _options.TcpOptions = new GangServerTcpOptions()
        {
            IpAddress = ipAddress,
            Port = port,
            Backlog = backlog
        };
        
        return this;
    }

    public GangServerOptionsBuilder WithSecretKeyValidation(string secretKey)
    {
        _options.SecretKeyValidation = true;
        _options.SecretKey = secretKey;
        
        return this;
    }

    public GangServerOptionsBuilder RequireUniqueClientIds()
    {
        _options.RequireUniqueClientIds = true;
        return this;
    }

    public GangServerOptionsBuilder CreateDefaultBuilder(IConfiguration configuration)
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
    
    public GangServerOptions Build()
    {
        return _options;
    }
}