using Microsoft.Extensions.Hosting;

namespace NetSync;

public interface INetworkService
{
    Task Run();
}

public class NetworkService : INetworkService
{
    private readonly IMessaging _messaging;
    private readonly Discovery _discovery;
    private readonly IHostApplicationLifetime _hostLifetime;
    private readonly Random _random = new();

    public NetworkService(IMessaging messaging, Discovery discovery, IHostApplicationLifetime hostLifetime)
    {
        _messaging = messaging;
        _discovery = discovery;
        _hostLifetime = hostLifetime;
        _discovery.OnHandout += (address) => _messaging.UpdateClient(address);
    }
    
    public async Task Run()
    {
        await _messaging.Start(_hostLifetime.ApplicationStopping);
        var listenTask = _messaging.Run(_hostLifetime.ApplicationStopping);
        var handoutTask = HandoutTask();
        
        await Task.WhenAll(_discovery.Run(), listenTask, handoutTask);
    }

    private async Task HandoutTask()
    {
        while (_hostLifetime.ApplicationStopping.IsCancellationRequested == false)
        {
            if(_messaging.EndPoint != null)
                await _discovery.Handout(_messaging.EndPoint.ToString());
            await Task.Delay(_random.Next(5000,10000), _hostLifetime.ApplicationStopping);
        }
    }
}