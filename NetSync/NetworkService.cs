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
        _discovery.OnHandout += (address) => _messaging.AddEndpoint(address);
    }
    
    public async Task Run()
    {
        await _messaging.Start(_hostLifetime.ApplicationStopping);
        var listenTask = _messaging.Listen(_hostLifetime.ApplicationStopping);
        var handoutTask = Task.Run(async () =>
        {
            while (_hostLifetime.ApplicationStopping.IsCancellationRequested == false)
            {
                await _discovery.Handout(_messaging.EndPoint!.ToString());
                await Task.Delay(_random.Next(5000,10000), _hostLifetime.ApplicationStopping);
            }
        }, _hostLifetime.ApplicationStopping);
        
        await Task.WhenAll(_discovery.Wait(), listenTask, handoutTask);
    }
}