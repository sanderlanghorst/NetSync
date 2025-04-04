using Google.Protobuf;

namespace NetSync;

public interface ISerializer
{
    byte[] Serialize<T>(T message) where T : IMessage<T>;
    T Deserialize<T>(byte[] message) where T : IMessage<T>;
}