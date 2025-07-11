using System.Net;
using Google.Protobuf;

namespace NetSync;

public interface IMessaging
{
    event EventHandler<IMessage> OnMessageReceived;
    event EventHandler<Client>? OnEndpointAdded;
    Task<NetworkInfo> Start(CancellationToken cancellationToken);
    Task Send<T>(T message, CancellationToken cancellationToken) where T : IMessage<T>;
    Task Send<T>(T message, Client endPoint, CancellationToken cancellationToken) where T : IMessage<T>;
    Task Run(CancellationToken cancellationToken);
    void UpdateClient(Client client);
    List<Client> Clients { get; }
    IPEndPoint? EndPoint { get; }
}