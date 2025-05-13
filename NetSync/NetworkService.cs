using System.Net;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetSync.Protos;

namespace NetSync;

public class NetworkService : IHostedService
{
    private readonly IMessaging _messaging;
    private readonly Discovery _discovery;

    private readonly Random _random = new();
    private readonly ILogger<NetworkService> _logger;
    private Task _listenTask = null!;
    private Task _discoveryTask = null!;
    private Task _handoutTask = null!;
    private CancellationTokenSource _cts = null!;

    private IPAddress[] LocalInterfaces { get; } = Dns.GetHostAddresses(Dns.GetHostName());

    public NetworkService(IMessaging messaging, Discovery discovery, ILogger<NetworkService> logger)
    {
        _logger = logger;
        _messaging = messaging;
        _discovery = discovery;
        _discovery.OnHandout += handout => _messaging.UpdateClient(handout.EndPoint);
    }

    private async Task HandoutTask(CancellationToken token)
    {
        while (token.IsCancellationRequested == false)
        {
            if (_messaging.EndPoint != null)
            {
                var localInterface =
                    LocalInterfaces.FirstOrDefault(i => i.AddressFamily == _messaging.EndPoint.AddressFamily)
                    ?? LocalInterfaces.First();
                await _discovery.Handout(
                    DiscoveryHandout.From(new IPEndPoint(localInterface, _messaging.EndPoint.Port)));
            }

            await Task.Delay(_random.Next(15000, 30000), token);
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            await _messaging.Start(_cts.Token);

            _listenTask = _messaging.Run(_cts.Token);
            _discoveryTask = _discovery.Run(_cts.Token);
            _handoutTask = HandoutTask(_cts.Token);
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "Error in NetworkService");
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _cts.CancelAsync();
        try
        {
            await Task.WhenAny(_listenTask, _discoveryTask, _handoutTask);
        }
        catch (OperationCanceledException)
        {
        }
    }
}