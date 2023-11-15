using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AppControl.Client.Helpers;

public class AppControlClientConnector
{
    public static async Task EasyConnectAsync(IServiceProvider serviceProvider)
    {
        var config = serviceProvider.GetRequiredService<IConfiguration>();
        var factory = new AppControlFactory(serviceProvider);
        var logger = serviceProvider.GetRequiredService<ILogger<AppControlClientConnector>>();

        var client = factory.CreateClient();

        var options = new AppControlClientOptionsBuilder()
            .CreateDefaultBuilder(config)
            .Build();

        client.MessageReceivedAsync += args =>
        {
            if (string.IsNullOrEmpty(args.Content))
            {
                return Task.CompletedTask;
            }
            
            logger.LogInformation($"Received: {args.Content}");
            return Task.CompletedTask;
        };

        client.OnAuthFailed += args =>
        {
            logger.LogError($"Failed authentication: {args.Reason}");
            
            Environment.Exit(0);
            return Task.CompletedTask;
        };

        await client.StartLongTermSessionAsync(options);
    }
}