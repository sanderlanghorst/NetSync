using System.Net;

namespace NetSync.Protos;

public sealed partial class DiscoveryHandout
{
    public static DiscoveryHandout From(IPEndPoint endPoint)
    {
        return new DiscoveryHandout
        {
            Address = endPoint.ToString()
        };
    }
}