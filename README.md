## Basic Usage

### Server

```cs
var config = host.Services.GetRequiredService<IConfiguration>();
var factory = new AppControlFactory(host.Services);

var options = new AppControlServerOptionsBuilder()
    .CreateDefaultBuilder(config)
    .Build();

using var server = factory.CreateServer(options);

await server.StartAsync();
await server.ListenAsync(CancellationToken.None);
```



### Client

```cs
var config = host.Services.GetRequiredService<IConfiguration>();
var factory = new AppControlFactory(host.Services);

using var client = factory.CreateClient();

var options = new AppControlSClientOptionsBuilder()
    .CreateDefaultBuilder(config)
    .Build();

await client.StartLongTermSessionAsync(options);
```
