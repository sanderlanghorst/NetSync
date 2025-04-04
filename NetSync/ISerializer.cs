using Google.Protobuf;

namespace NetSync;

public interface ISerializer
{
    byte[] Encode<T>(T message) where T : IMessage<T>;
    T Decode<T>(byte[] message) where T : IMessage<T>;
}