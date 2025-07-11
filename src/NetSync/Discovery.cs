using System.Net;
using System.Net.Sockets;
using System.Text;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetSync.Protos;

namespace NetSync;

public interface IDiscovery
{
    event Action<DiscoveryRecieved>? OnHandout;
    Task Run(NetworkInfo info, CancellationToken cancellationToken);
    Task Shout();
    Task Ping(DiscoveryPing ping);
}
internal class Discovery : IDiscovery
{
    private readonly ISerializer _serializer;
    private readonly IOptions<NetSyncOptions> _options;
    
    private Task _listenTask = null!;
    private UdpClient _udpChannel = null!;
    private NetworkInfo? _info = null;
    private readonly string _uniqueId;
    private readonly Encoding _localEncoding = Encoding.ASCII;
    private readonly ILogger<Discovery> _logger;

    public event Action<DiscoveryRecieved>? OnHandout;

    public Discovery(ISerializer serializer, IOptions<NetSyncOptions> options, ILogger<Discovery> logger)
    {
        _serializer = serializer;
        _options = options;
        _logger = logger;
        _uniqueId = Guid.CreateVersion7().ToString("N").Substring(16, 16);
    }

    public async Task Run(NetworkInfo info, CancellationToken cancellationToken)
    {
        _info = info;
        _logger.LogInformation("I am {Id}", _uniqueId);
        _udpChannel = new UdpClient();
        _udpChannel.EnableBroadcast = true;
        _udpChannel.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _udpChannel.Client.Bind(new IPEndPoint(IPAddress.Any, _options.Value.DiscoveryPort));
        _listenTask = ListenTask(cancellationToken);
        cancellationToken.Register(() =>
        {
            _udpChannel.Close();
        });

        await Task.WhenAll(_listenTask);
    }

    private async Task ListenTask(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Listening on port " + _options.Value.DiscoveryPort);
        
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var response = await _udpChannel.ReceiveAsync(cancellationToken);
                var any = Any.Parser.ParseFrom(response.Buffer);
                var message = any.ToRegisteredType();

                await HandleProtocol(message);
            }
            catch (SocketException e)
            {
                _logger.LogError(e, e.Message);
            }
        }
    }

    private Task HandleProtocol(IMessage message)
    {
        return message switch
        {
            DiscoveryShout shout => HandleShout(shout),
            DiscoveryResponse response => HandleResponse(response),
            DiscoveryPing ping => HandlePing(ping),
            _ => Task.CompletedTask
        };
    }

    private Task HandleResponse(DiscoveryResponse response)
    {
        // filter out own messages
        if (response.Id.Equals(_uniqueId))
        {
            return Task.CompletedTask;
        }
        
        var endPoint = IPEndPoint.Parse(response.Address);

        OnHandout?.Invoke(new DiscoveryRecieved(new Client(response.Id, endPoint)));
        return Task.CompletedTask;
    }

    private Task HandlePing(DiscoveryPing ping)
    {
        return Task.CompletedTask;
    }

    private Task HandleShout(DiscoveryShout shout)
    {
        // filter out own messages
        if (shout.Id.Equals(_uniqueId))
        {
            return Task.CompletedTask;
        }
        
        var endPoint = IPEndPoint.Parse(shout.Address);

        OnHandout?.Invoke(new DiscoveryRecieved(new Client(shout.Id, endPoint)));
        return ShoutBack();
    }


    public async Task Shout()
    {
        if (_info is null) return;
        
        var discovery = new DiscoveryShout()
        {
            Id = _uniqueId,
            Address = _info.EndPoint.ToString(),
        };
        
        _logger.LogDebug("Handout for " + discovery);
        var message = Any.Pack(discovery).Serialize();
        var broadcastEndPoint = new IPEndPoint(IPAddress.Broadcast, _options.Value.DiscoveryPort);
        await _udpChannel.SendAsync(message, message.Length, broadcastEndPoint);
    }

    private async Task ShoutBack()
    {
        if (_info is null) return;
        
        var response = new DiscoveryResponse
        {
            Id = _uniqueId,
            Address = _info.EndPoint.ToString()
        };
        
        var message = Any.Pack(response).Serialize();
        var broadcastEndPoint = new IPEndPoint(IPAddress.Broadcast, _options.Value.DiscoveryPort);
        await _udpChannel.SendAsync(message, message.Length, broadcastEndPoint);
    }
    public async Task Ping(DiscoveryPing ping)
    {
        if (ping is null) throw new ArgumentNullException(nameof(ping));
        
        var message = Any.Pack(ping).Serialize();
        var broadcastEndPoint = new IPEndPoint(IPAddress.Broadcast, _options.Value.DiscoveryPort);
        await _udpChannel.SendAsync(message, message.Length, broadcastEndPoint);
    }
}

public record DiscoveryRecieved(Client Client);