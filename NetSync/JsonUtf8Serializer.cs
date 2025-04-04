using System.Text;
using System.Text.Json;
using Google.Protobuf;

namespace NetSync;

public class JsonUtf8Serializer : ISerializer
{
    public byte[] Serialize<T>(T message) where T : IMessage<T>
    {
        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
    }

    public T Deserialize<T>(byte[] message) where T : IMessage<T>
    {
        return JsonSerializer.Deserialize<T>(Encoding.UTF8.GetString(message))!;
    }
}