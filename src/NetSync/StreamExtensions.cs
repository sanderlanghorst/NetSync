namespace NetSync;

public static class StreamExtensions
{
    public static async Task<byte[]> ReadAllBytesAsync(this Stream stream)
    {
        using (var memoryStream = new MemoryStream())
        {
            await stream.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }
    }
}