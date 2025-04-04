using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Hosting;

namespace NetSync;

public class Discovery
{
    private readonly IHostApplicationLifetime _hostLifetime;
    private readonly int _port = 12345;
    private Task _listenTask;
    private readonly UdpClient _udpChannel;
    private readonly string _uniqueId;

    public event Action<IPEndPoint> OnHandout;

    private IPAddress[] LocalInterface { get; } = Dns.GetHostAddresses(Dns.GetHostName());

    public Discovery(IHostApplicationLifetime hostLifetime)
    {
        _hostLifetime = hostLifetime;
        _udpChannel = new UdpClient();
        _udpChannel.EnableBroadcast = true;
        _udpChannel.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _uniqueId = Guid.CreateVersion7().ToString("N");
    }

    public async Task Run()
    {
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
        try
        {
            Console.WriteLine("Discovery listening on port " + _port);
            while (true)
            {
                var response = await _udpChannel.ReceiveAsync(_hostLifetime.ApplicationStopping);
                var message = Encoding.ASCII.GetString(response.Buffer, 0, response.Buffer.Length);
                if (message.StartsWith(_uniqueId))
                {
                    continue;
                }

                var address = IPEndPoint.Parse(message.Substring(_uniqueId.Length));

                OnHandout?.Invoke(new IPEndPoint(response.RemoteEndPoint.Address, address.Port));
            }
        }
        catch (SocketException e)
        {
            Console.WriteLine(e);
        }
    }

    public async Task Handout(string address)
    {
        var message = Encoding.ASCII.GetBytes($"{_uniqueId}{address}");
        var broadcastEndPoint = new IPEndPoint(IPAddress.Broadcast, _port);
        await _udpChannel.SendAsync(message, message.Length, broadcastEndPoint);
    }
}