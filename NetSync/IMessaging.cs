using System.Net;
using Google.Protobuf;

namespace NetSync;

public interface IMessaging
{
    event EventHandler<IMessage> OnMessageReceived;
    Task Start(CancellationToken cancellationToken);
    Task Send<T>(T message, CancellationToken cancellationToken) where T:IMessage<T>;
    Task Run(CancellationToken cancellationToken);
    void UpdateClient(IPEndPoint address);
    List<IPEndPoint> EndPoints { get; }
    IPEndPoint? EndPoint { get; }
}