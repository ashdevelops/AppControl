using Microsoft.Extensions.DependencyInjection;
using AppControl.Client;
using AppControl.Server;

namespace AppControl;

public class AppControlFactory
{
    private readonly IServiceProvider _serviceProvider;

    public AppControlFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public AppControlClient CreateClient()
    {
        return ActivatorUtilities.CreateInstance<AppControlClient>(_serviceProvider);
    }

    public AppControlServer CreateServer(AppControlServerOptions options)
    {
        var clientRepository = ActivatorUtilities.CreateInstance<AppControlServerClientRepository>(_serviceProvider);
        return ActivatorUtilities.CreateInstance<AppControlServer>(_serviceProvider, options, clientRepository);
    }
}