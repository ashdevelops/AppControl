using AppControl.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Paz.SharedSupport.Database.Dao;

namespace AppControl;

public class AppControlServiceCollection
{
    public static void AddServices(IServiceCollection serviceCollection, IConfiguration config)
    {
        serviceCollection.AddSingleton<ApplicationDao>();
        serviceCollection.AddSingleton<AppControlFactory>();
        serviceCollection.AddSingleton<AppControlServerClientRepository>();
    }
}