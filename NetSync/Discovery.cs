using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Hosting;

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

    public event Action<DiscoveryRecieved> OnHandout;

    public Discovery(IHostApplicationLifetime hostLifetime, ISerializer serializer)
    {
        _hostLifetime = hostLifetime;
        _serializer = serializer;
        
        _uniqueId = Guid.CreateVersion7().ToString("N");
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
        Console.WriteLine("Discovery listening on port " + _port);
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

                var handout = Decode<DiscoveryHandout>(response.Buffer);
                var endPoint = IPEndPoint.Parse(handout.Address);

                OnHandout?.Invoke(new DiscoveryRecieved(endPoint));
            }
            catch (SocketException e)
            {
                Console.WriteLine(e);
            }
        }
    }

    public async Task Handout(DiscoveryHandout discovery)
    {
        Console.WriteLine("Handout discovery to " + discovery.Address);
        var message = Encode(discovery);
        var broadcastEndPoint = new IPEndPoint(IPAddress.Broadcast, _port);
        await _udpChannel.SendAsync(message, message.Length, broadcastEndPoint);
    }

    private byte[] Encode<T>(T data)
    {
        var dataBytes = _serializer.Encode(data);
        var uniqueIdBytes = _localEncoding.GetBytes(_uniqueId);
        var result = new byte[uniqueIdBytes.Length + dataBytes.Length];

        Array.Copy(uniqueIdBytes, 0, result, 0, uniqueIdBytes.Length);
        Array.Copy(dataBytes, 0, result, uniqueIdBytes.Length, dataBytes.Length);

        return result;
    }

    public T Decode<T>(byte[] message)
    {
        var uniqueIdLength = _localEncoding.GetByteCount(_uniqueId);
        var dataBytes = new byte[message.Length - uniqueIdLength];
        Array.Copy(message, uniqueIdLength, dataBytes, 0, dataBytes.Length);
        return _serializer.Decode<T>(dataBytes);
    }
}

public record DiscoveryHandout
{
    public required string Address { get; init; }

    public static DiscoveryHandout From(IPEndPoint endPoint)
    {
        return new DiscoveryHandout
        {
            Address = endPoint.ToString()
        };
    }
}

public record DiscoveryRecieved(IPEndPoint EndPoint);