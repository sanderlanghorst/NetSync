using System.Net;
using System.Net.Sockets;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using NetSync.Protos;

namespace NetSync;

public class Messaging : IMessaging
{
    private readonly TcpListener _tcpListener;
    private readonly ILogger<Messaging> _logger;
    public IPEndPoint? EndPoint { get; private set; }

    public List<IPEndPoint> EndPoints { get; } = [];

    private bool IsStarted => EndPoint != null;

    public Messaging(ILogger<Messaging> logger)
    {
        _tcpListener = new TcpListener(IPAddress.Any, 0);
        _logger = logger;
    }

    public event EventHandler<IMessage>? OnMessageReceived;
    public event EventHandler<IPEndPoint>? OnEndpointAdded;

    public Task Start(CancellationToken cancellationToken)
    {
        _tcpListener.Start();
        EndPoint = (IPEndPoint)_tcpListener.LocalEndpoint;
        cancellationToken.Register(() => _tcpListener.Stop());

        return Task.CompletedTask;
    }

    public async Task Send<T>(T message, CancellationToken cancellationToken) where T : IMessage<T>
    {
        if (!IsStarted) throw new InvalidOperationException("Not started");
        var sendingTasks = EndPoints.ToList().Select(async e => await SendMessage(e, message, cancellationToken))
            .ToArray();
        await Task.WhenAll(sendingTasks);
    }

    public async Task Send<T>(T message, IPEndPoint endPoint, CancellationToken cancellationToken) where T : IMessage<T>
    {
        if (!IsStarted) throw new InvalidOperationException("Not started");
        var sendingTasks = EndPoints.Where(e => Equals(e, endPoint)).ToList()
            .Select(async e => await SendMessage(e, message, cancellationToken))
            .ToArray();
        await Task.WhenAll(sendingTasks);
    }

    private async Task SendMessage<T>(IPEndPoint endPoint, T message, CancellationToken cancellationToken)
        where T : IMessage<T>
    {
        try
        {
            var client = new TcpClient();
            await client.ConnectAsync(endPoint, cancellationToken);
            var bytes = Serialize(message);
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
                EndPoints.Remove(endPoint);
                _logger.LogDebug($"Removed endpoint {endPoint} due to error: {e.SocketErrorCode}");
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
        }
    }
    
    public byte[] Serialize<T>(T message) where T : IMessage<T>
    {
        if (message is IMessage protobufMessage)
        {
            using (var memoryStream = new MemoryStream())
            {
                protobufMessage.WriteTo(memoryStream);
                return memoryStream.ToArray();
            }
        }

        throw new InvalidOperationException("Message must be a protobuf message.");
    }

    public T Deserialize<T>(byte[] message) where T : IMessage<T>
    {
        var parser = (MessageParser<T>?)typeof(T).GetProperty("Parser")?.GetValue(null);
        if (parser == null) throw new InvalidOperationException($"Type {typeof(T).Name} does not have a valid parser.");
        return parser.ParseFrom(message);
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
                var message = Deserialize<MessageSync>(bytes);

                OnMessageReceived?.Invoke(this, message);
                client.Close();
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }
    }

    public void UpdateClient(IPEndPoint address)
    {
        if (!EndPoints.Any(ep => ep.Equals(address)))
        {
            EndPoints.Add(address);
            _logger.LogInformation("Messaging added endpoint: {0}", address);
            OnEndpointAdded?.Invoke(this, address);
        }
    }
}
