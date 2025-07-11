using System.Net;
using System.Net.Sockets;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using NetSync.Protos;

namespace NetSync;

internal class Messaging : IMessaging
{
    private readonly TcpListener _tcpListener;
    private readonly ILogger<Messaging> _logger;
    public IPEndPoint? EndPoint { get; private set; }

    public List<Client> Clients { get; } = [];

    private bool IsStarted => EndPoint != null;

    public Messaging(ILogger<Messaging> logger)
    {
        _tcpListener = new TcpListener(IPAddress.Any, 0);
        _logger = logger;
    }

    public event EventHandler<IMessage>? OnMessageReceived;
    public event EventHandler<Client>? OnEndpointAdded;

    public async Task<NetworkInfo> Start(CancellationToken cancellationToken)
    {
        _tcpListener.Start();
        EndPoint = (IPEndPoint)_tcpListener.LocalEndpoint;
        cancellationToken.Register(() => _tcpListener.Stop());

        var localInterfaces = await Dns.GetHostAddressesAsync(Dns.GetHostName(), cancellationToken);
        var localInterface =
            localInterfaces.FirstOrDefault(i => i.AddressFamily == EndPoint.AddressFamily)
            ?? localInterfaces.First();
        
        return new NetworkInfo(new IPEndPoint(localInterface, EndPoint.Port));
    }

    public async Task Send<T>(T message, CancellationToken cancellationToken) where T : IMessage<T>
    {
        if (!IsStarted) throw new InvalidOperationException("Not started");
        var sendingTasks = Clients.ToList().Select(async e => await SendMessage(e, message, cancellationToken))
            .ToArray();
        await Task.WhenAll(sendingTasks);
    }

    public async Task Send<T>(T message, Client endPoint, CancellationToken cancellationToken) where T : IMessage<T>
    {
        if (!IsStarted) throw new InvalidOperationException("Not started");
        var sendingTasks = Clients.Where(e => Equals(e, endPoint)).ToList()
            .Select(async e => await SendMessage(e, message, cancellationToken))
            .ToArray();
        await Task.WhenAll(sendingTasks);
    }

    private async Task SendMessage<T>(Client endPoint, T message, CancellationToken cancellationToken)
        where T : IMessage<T>
    {
        try
        {
            var client = new TcpClient();
            await client.ConnectAsync(endPoint.Endpoint, cancellationToken);
            var m = Any.Pack(message);
            var bytes = m.Serialize();
            await client.GetStream().WriteAsync(bytes, 0, bytes.Length, cancellationToken);
            client.Close();
        }
        catch (SocketException e)
        {
            if (e.SocketErrorCode == SocketError.ConnectionRefused ||
                e.SocketErrorCode == SocketError.HostNotFound ||
                e.SocketErrorCode == SocketError.NetworkUnreachable ||
                e.SocketErrorCode == SocketError.TimedOut)
            {
                Clients.Remove(endPoint);
                _logger.LogDebug($"Removed endpoint {endPoint} due to error: {e.SocketErrorCode}");
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
        }
    }

    public async Task Run(CancellationToken cancellationToken)
    {
        if (!IsStarted) throw new InvalidOperationException("Not started");

        cancellationToken.Register(() => _tcpListener.Stop());
        _logger.LogInformation("Listening on port {0}", EndPoint!.Port);
        
        while (cancellationToken.IsCancellationRequested == false)
        {
            try
            {
                var client = await _tcpListener.AcceptTcpClientAsync(cancellationToken);
                var bytes = await client.GetStream().ReadAllBytesAsync();
                var any = Any.Parser.ParseFrom(bytes);
                var message = any.ToRegisteredType();

                OnMessageReceived?.Invoke(this, message);
                client.Close();
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }
    }

    public void UpdateClient(Client client)
    {
        if (!Clients.Any(ep => ep.Endpoint.Equals(client.Endpoint)))
        {
            Clients.Add(client);
            _logger.LogInformation("Messaging added endpoint: {0}", client.Id);
            OnEndpointAdded?.Invoke(this, client);
        }
    }
}
