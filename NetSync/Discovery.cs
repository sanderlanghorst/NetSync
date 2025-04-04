using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Hosting;

namespace NetSync;

public class Discovery
{
    private int _port = 12345;
    private Task _listenTask;
    private UdpClient _listener;

    public Discovery(IHostApplicationLifetime hostLifetime)
    {
        _listener = new UdpClient();
        _listener.EnableBroadcast = true;
        _listener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _listener.Client.Bind(new IPEndPoint(IPAddress.Any, _port));
        _listenTask = Task.Run(async () =>
        {
            try
            {
                Console.WriteLine("Discovery listening on port " + _port);
                while (true)
                {
                    var response = await _listener.ReceiveAsync(hostLifetime.ApplicationStopping);
                    var message = Encoding.ASCII.GetString(response.Buffer, 0, response.Buffer.Length);
                    if (!message.Contains(".") || !message.Contains(":") || !message.StartsWith("[") ||
                        !message.EndsWith("]"))
                    {
                        continue;
                    }
                    var address = IPEndPoint.Parse(message.Substring(1, message.Length - 2));
                    OnHandout?.Invoke(new IPEndPoint(response.RemoteEndPoint.Address, address.Port));
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine(e);
            }
        });
    }
    
    public async Task Handout(string address)
    {
        var message = Encoding.ASCII.GetBytes($"[{address}]");
        var broadcastEndPoint = new IPEndPoint(IPAddress.Broadcast, _port);
        await _listener.SendAsync(message, message.Length, broadcastEndPoint);
    }

    public async Task Wait()
    {   
        await _listenTask;
    }

    public event Action<IPEndPoint> OnHandout;
}