using Microsoft.Extensions.DependencyInjection;
using AppControl.Client;
using AppControl.Server;

namespace AppControl;

public class GangFactory
{
    private readonly IServiceProvider _serviceProvider;

    public GangFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public GangClient CreateClient()
    {
        return ActivatorUtilities.CreateInstance<GangClient>(_serviceProvider);
    }

    public GangServer CreateServer(GangServerOptions options)
    {
        var clientRepository = ActivatorUtilities.CreateInstance<GangServerClientRepository>(_serviceProvider);
        return ActivatorUtilities.CreateInstance<GangServer>(_serviceProvider, options, clientRepository);
    }
}