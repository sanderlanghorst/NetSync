using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NetSync;

public class Messaging : IMessaging
{
    private readonly TcpListener _tcpListener;
    public IPEndPoint? EndPoint { get; private set; }

    public List<IPEndPoint> EndPoints { get; } = [];

    private bool IsStarted => EndPoint != null;
    
    public Messaging()
    {
        _tcpListener = new TcpListener(IPAddress.Any, 0);
    }
    
    public Task Start(CancellationToken cancellationToken)
    {
        _tcpListener.Start();
        EndPoint = (IPEndPoint)_tcpListener.LocalEndpoint;
        cancellationToken.Register(() => _tcpListener.Stop());
        
        return Task.CompletedTask;
    }
    
    public async Task Send(string say, CancellationToken cancellationToken)
    {
        if(!IsStarted) throw new InvalidOperationException("Not started");
        var sendingTasks = EndPoints.ToList().Select(async (e) => await SendMessage(e, say, cancellationToken)).ToArray();
        await Task.WhenAll(sendingTasks);
    }

    private async Task SendMessage(IPEndPoint endPoint, string message, CancellationToken cancellationToken)
    {
        try
        {
            var client = new TcpClient();
            await client.ConnectAsync(endPoint, cancellationToken);
            var bytes = Encoding.ASCII.GetBytes(message);
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
                Console.WriteLine($"Removed endpoint {endPoint} due to error: {e.SocketErrorCode}");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
    
    public async Task Run(CancellationToken cancellationToken)
    {
        if(!IsStarted) throw new InvalidOperationException("Not started");
        
        cancellationToken.Register(() => _tcpListener.Stop());
        Console.WriteLine("Messaging listening on port {0}", EndPoint!.Port);
        while (cancellationToken.IsCancellationRequested == false)
        {
            var client = await _tcpListener.AcceptTcpClientAsync(cancellationToken);
            var message = new StringBuilder();
            using var reader = new StreamReader(client.GetStream(), Encoding.ASCII);
            char[] buffer = new char[1024];
            int bytesRead;
            while ((bytesRead = await reader.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                message.Append(buffer, 0, bytesRead);
            }
            
            Console.WriteLine("Messaging Received: {0}", message);
            client.Close();
        }
    }

    public void UpdateClient(IPEndPoint address)
    {
        
        if (!EndPoints.Any(ep => ep.Equals(address)))
        {
            EndPoints.Add(address);
            Console.WriteLine("Messaging added endpoint: {0}", address);
        }
    }
    
    
}