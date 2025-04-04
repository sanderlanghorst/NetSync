using System.Net;
using System.Net.Sockets;
using System.Text;
using Google.Protobuf;
using Microsoft.Extensions.Logging;

namespace NetSync;

public class Messaging : IMessaging
{
    private readonly TcpListener _tcpListener;
    private readonly ISerializer _serializer;
    private readonly ILogger<Messaging> _logger;
    public IPEndPoint? EndPoint { get; private set; }

    public List<IPEndPoint> EndPoints { get; } = [];

    private bool IsStarted => EndPoint != null;
    
    public Messaging(ISerializer serializer, ILogger<Messaging> logger)
    {
        _tcpListener = new TcpListener(IPAddress.Any, 0);
        _serializer = serializer;
        _logger = logger;
    }

    public event EventHandler<IMessage>? OnMessageReceived;

    public Task Start(CancellationToken cancellationToken)
    {
        _tcpListener.Start();
        EndPoint = (IPEndPoint)_tcpListener.LocalEndpoint;
        cancellationToken.Register(() => _tcpListener.Stop());
        
        return Task.CompletedTask;
    }

    public async Task Send<T>(T message, CancellationToken cancellationToken) where T : IMessage<T>
    {
        if(!IsStarted) throw new InvalidOperationException("Not started");
        var sendingTasks = EndPoints.ToList().Select(async (e) => await SendMessage(e, message, cancellationToken)).ToArray();
        await Task.WhenAll(sendingTasks);
    }

    private async Task SendMessage<T>(IPEndPoint endPoint, T message, CancellationToken cancellationToken) where T:IMessage<T>
    {
        try
        {
            var client = new TcpClient();
            await client.ConnectAsync(endPoint, cancellationToken);
            var bytes= _serializer.Serialize(message);
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
                _logger.LogInformation($"Removed endpoint {endPoint} due to error: {e.SocketErrorCode}");
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
        }
    }
    
    public async Task Run(CancellationToken cancellationToken)
    {
        if(!IsStarted) throw new InvalidOperationException("Not started");
        
        cancellationToken.Register(() => _tcpListener.Stop());
        _logger.LogInformation("Messaging listening on port {0}", EndPoint!.Port);
        while (cancellationToken.IsCancellationRequested == false)
        {
            try
            {
                var client = await _tcpListener.AcceptTcpClientAsync(cancellationToken);
                
                var message = _serializer.Deserialize<>()
                
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
        }
    }
    
    
}