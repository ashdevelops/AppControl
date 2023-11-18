using AppControl.Other;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AppControl.Client.Helpers;

public class AppControlClientConnector
{
    public static async Task EasyConnectAsync(IServiceProvider serviceProvider)
    {
        if (!File.Exists("/usr/local/bin/start-application") || 
            !File.Exists("/usr/local/bin/stop-application"))
        {
            throw new Exception("Ensure system-wide application commands are registered before running.");
        }
        
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

            var packet = JsonConvert.DeserializeObject<NetworkPacket>(args.Content);

            if (packet != null)
            {
                switch (packet.Name)
                {
                    case "DisposedWithReason":
                        Environment.Exit(0);
                        break;
                }
            }
            
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