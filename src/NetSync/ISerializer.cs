namespace NetSync;

public interface ISerializer
{
    byte[] Serialize<T>(T message);
    object Deserialize(byte[] message, Type type);
}