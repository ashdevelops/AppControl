namespace AppControl.Client;

public class GangClientPinger
{
    private readonly GangClient _client;

    public GangClientPinger(GangClient client)
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