namespace AppControl.Client;

public class AppControlClientPinger
{
    private readonly AppControlClient _client;

    public AppControlClientPinger(AppControlClient client)
    {
        _client = client;
    }

    public async Task StartAsync()
    {
        await Task.Factory.StartNew(PeriodicallyPingAsync, TaskCreationOptions.LongRunning);
    }

    private async Task PeriodicallyPingAsync()
    {
        while (true)
        {
            await Task.Delay(1000);
            await _client.SendPingAsync();
        }
    }
}