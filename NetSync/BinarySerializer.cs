using Google.Protobuf;

namespace NetSync;

public class BinarySerializer:ISerializer
{
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
        var parser = (MessageParser<T>)typeof(T).GetProperty("Parser").GetValue(null);
        return parser.ParseFrom(message);
    }
}