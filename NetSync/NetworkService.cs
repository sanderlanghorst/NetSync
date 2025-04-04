using System.Net;
using Microsoft.Extensions.Hosting;
using NetSync.Protos;

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
    
    private IPAddress[] LocalInterfaces { get; } = Dns.GetHostAddresses(Dns.GetHostName());

    public NetworkService(IMessaging messaging, Discovery discovery, IHostApplicationLifetime hostLifetime)
    {
        _messaging = messaging;
        _discovery = discovery;
        _hostLifetime = hostLifetime;
        _discovery.OnHandout += handout => _messaging.UpdateClient(handout.EndPoint);
    }

    public async Task Run()
    {
        await _messaging.Start(_hostLifetime.ApplicationStopping);
        
        var listenTask = _messaging.Run(_hostLifetime.ApplicationStopping);
        
        await Task.WhenAll(_discovery.Run(), listenTask, HandoutTask());
    }

    private async Task HandoutTask()
    {
        while (_hostLifetime.ApplicationStopping.IsCancellationRequested == false)
        {
            if (_messaging.EndPoint != null)
            {
                var localInterface =
                    LocalInterfaces.FirstOrDefault(i => i.AddressFamily == _messaging.EndPoint.AddressFamily)
                    ?? LocalInterfaces.First();
                await _discovery.Handout(DiscoveryHandout.From(new IPEndPoint(localInterface, _messaging.EndPoint.Port)));
            }
            await Task.Delay(_random.Next(5000, 10000), _hostLifetime.ApplicationStopping);
        }
    }
}