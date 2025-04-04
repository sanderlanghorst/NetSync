using System.Net;

namespace NetSync;

public interface IMessaging
{
    Task Start(CancellationToken cancellationToken);
    Task Send(string say, CancellationToken cancellationToken);
    Task Run(CancellationToken cancellationToken);
    void UpdateClient(IPEndPoint address);
    List<IPEndPoint> EndPoints { get; }
    IPEndPoint? EndPoint { get; }
}