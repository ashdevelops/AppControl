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
        return ActivatorUtilities.CreateInstance<AppControlServer>(_serviceProvider, options);
    }
}