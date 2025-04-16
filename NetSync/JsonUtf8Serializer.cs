using System.Text;
using System.Text.Json;
using Google.Protobuf;

namespace NetSync;

public class JsonUtf8Serializer : ISerializer
{
    public byte[] Serialize<T>(T message)
    {
        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
    }

    public object Deserialize(byte[] message, Type type)
    {
        return JsonSerializer.Deserialize(Encoding.UTF8.GetString(message), type)!;
    }
}