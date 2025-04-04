namespace NetSync;

public interface ISerializer
{
    byte[] Encode<T>(T message);
    T Decode<T>(byte[] message);
}