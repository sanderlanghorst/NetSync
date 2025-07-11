using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace NetSync.Protos;

public static class MessageExtensions
{
    public static byte[] Serialize(this IMessage message)
    {
        using (var memoryStream = new MemoryStream())
        {
            message.WriteTo(memoryStream);
            return memoryStream.ToArray();
        }
    }

    public static IMessage ToRegisteredType(this Any any)
    {
        if (any.TryUnpack(out DiscoveryShout shout)) return shout;
        if (any.TryUnpack(out DiscoveryResponse shoutResponse)) return shoutResponse;
        if (any.TryUnpack(out DiscoveryPing ping)) return ping;
        if (any.TryUnpack(out AskForUpdate askForUpdate)) return askForUpdate;
        if (any.TryUnpack(out ResponseUpdate responseUpdate)) return responseUpdate;
        if (any.TryUnpack(out Data data)) return data;

        throw new InvalidOperationException("Unsupported type in Any: " + any.TypeUrl);
    }
}