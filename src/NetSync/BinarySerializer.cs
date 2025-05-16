namespace NetSync;

public class BinarySerializer : ISerializer
{
    public byte[] Serialize<T>(T message) 
    {
        throw new NotImplementedException();
    }
    
    public object Deserialize(byte[] message, Type type)
    {
        throw new NotImplementedException();
    }
}