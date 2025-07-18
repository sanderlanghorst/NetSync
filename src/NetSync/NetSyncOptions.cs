namespace NetSync;

public class NetSyncOptions
{
    public bool ManualStart { get; set; }
    public Action<CancellationToken>? Start { get; set; }
    public Action? Stop { get; set; }
    public int DiscoveryPort { get; set; } = 5000;
}