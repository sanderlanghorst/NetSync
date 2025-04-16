using System.Net;
using System.Net.Sockets;
using System.Text;
using Google.Protobuf;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetSync.Protos;

namespace NetSync;

public class Discovery
{
    private readonly IHostApplicationLifetime _hostLifetime;
    private readonly ISerializer _serializer;
    private readonly int _port = 12345;
    private Task _listenTask;
    private UdpClient _udpChannel;
    private readonly string _uniqueId;
    private readonly Encoding _localEncoding = Encoding.ASCII;
    private readonly ILogger<Discovery> _logger;

    public event Action<DiscoveryRecieved> OnHandout;

    public Discovery(IHostApplicationLifetime hostLifetime, ISerializer serializer, ILogger<Discovery> logger)
    {
        _hostLifetime = hostLifetime;
        _serializer = serializer;
        _logger = logger;
        _uniqueId = Guid.CreateVersion7().ToString("N").Substring(16, 16);
    }

    public async Task Run()
    {
        _udpChannel = new UdpClient();
        _udpChannel.EnableBroadcast = true;
        _udpChannel.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _udpChannel.Client.Bind(new IPEndPoint(IPAddress.Any, _port));
        _listenTask = ListenTask();
        _hostLifetime.ApplicationStopping.Register(() =>
        {
            _udpChannel.Close();
            _listenTask.Wait();
        });

        await Task.WhenAll(_listenTask);
    }

    private async Task ListenTask()
    {
        _logger.LogInformation("Listening on port " + _port);
        
        while (!_hostLifetime.ApplicationStopping.IsCancellationRequested)
        {
            try
            {
                var response = await _udpChannel.ReceiveAsync(_hostLifetime.ApplicationStopping);
                var recievedId = _localEncoding.GetString(response.Buffer, 0, _localEncoding.GetByteCount(_uniqueId));
                if (recievedId.Equals(_uniqueId))
                {
                    continue;
                }
                _logger.LogDebug("Recieved handout from " + response.RemoteEndPoint);

                var handout = Decode<DiscoveryHandout>(response.Buffer);
                var endPoint = IPEndPoint.Parse(handout.Address);

                OnHandout?.Invoke(new DiscoveryRecieved(endPoint));
            }
            catch (SocketException e)
            {
                _logger.LogError(e, e.Message);
            }
        }
    }

    public async Task Handout(DiscoveryHandout discovery)
    {
        _logger.LogDebug("Handout to " + discovery.Address);
        var message = Encode(discovery);
        var broadcastEndPoint = new IPEndPoint(IPAddress.Broadcast, _port);
        await _udpChannel.SendAsync(message, message.Length, broadcastEndPoint);
    }

    private byte[] Encode<T>(T data) where T : IMessage<T>
    {
        var dataBytes = _serializer.Serialize(data);
        var uniqueIdBytes = _localEncoding.GetBytes(_uniqueId);
        var result = new byte[uniqueIdBytes.Length + dataBytes.Length];

        Array.Copy(uniqueIdBytes, 0, result, 0, uniqueIdBytes.Length);
        Array.Copy(dataBytes, 0, result, uniqueIdBytes.Length, dataBytes.Length);

        return result;
    }

    private T Decode<T>(byte[] message) where T : IMessage<T>
    {
        var uniqueIdLength = _localEncoding.GetByteCount(_uniqueId);
        var dataBytes = new byte[message.Length - uniqueIdLength];
        Array.Copy(message, uniqueIdLength, dataBytes, 0, dataBytes.Length);
        return (T)_serializer.Deserialize(dataBytes, typeof(T));
    }
}

public record DiscoveryRecieved(IPEndPoint EndPoint);