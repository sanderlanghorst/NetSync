using System.Net;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetSync.Protos;

namespace NetSync;

public class NetworkService : IHostedService
{
    private readonly IMessaging _messaging;
    private readonly IDiscovery _discovery;

    private readonly Random _random = new();
    private readonly ILogger<NetworkService> _logger;
    private readonly IOptions<NetSyncOptions> _options;
    private Task _listenTask = null!;
    private Task _discoveryTask = null!;
    private Task _handoutTask = null!;
    private CancellationTokenSource _cts = null!;

    public NetworkService(IMessaging messaging, IDiscovery discovery, ILogger<NetworkService> logger, IOptions<NetSyncOptions> options)
    {
        _logger = logger;
        _options = options;
        _messaging = messaging;
        _discovery = discovery;
        if (_options.Value.ManualStart)
        {
            _options.Value.Start += (ct) => StartAsync(ct).Wait();
            _options.Value.Stop += () => StopAsync().Wait();
        }
        _discovery.OnHandout += handout => _messaging.UpdateClient(handout.Client);
    }

    private async Task HandoutTask(CancellationToken token)
    {
        while (token.IsCancellationRequested == false)
        {
            if (_messaging.EndPoint != null)
            {
                await _discovery.Shout();
            }

            await Task.Delay(_random.Next(15000, 30000), token);
        }
    }

    async Task IHostedService.StartAsync(CancellationToken cancellationToken)
    {
        if (_options.Value.ManualStart) return;
        await StartAsync(cancellationToken);
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        try
        {
            var info = await _messaging.Start(_cts.Token);

            _listenTask = _messaging.Run(_cts.Token);
            _discoveryTask = _discovery.Run(info, _cts.Token);
            _handoutTask = HandoutTask(_cts.Token);
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "Error in NetworkService");
        }
    }

    async Task IHostedService.StopAsync(CancellationToken cancellationToken)
    {
        await StopAsync();
    }

    public async Task StopAsync()
    {
        try
        {
            await _cts.CancelAsync();
            await Task.WhenAny(_listenTask, _discoveryTask, _handoutTask);
        }
        catch (OperationCanceledException)
        {
        }
    }
}