using System.Text;
using System.Text.Json;

namespace NetSync;

public class JsonUtf8Serializer : ISerializer
{
    public byte[] Encode<T>(T message)
    {
        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
    }

    public T Decode<T>(byte[] message)
    {
        return JsonSerializer.Deserialize<T>(Encoding.UTF8.GetString(message))!;
    }
}