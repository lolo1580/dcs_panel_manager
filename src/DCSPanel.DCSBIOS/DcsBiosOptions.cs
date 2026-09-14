using System.Net;

namespace DCSPanel.DCSBIOS;

public sealed class DcsBiosOptions
{
    public IPAddress MulticastAddress { get; init; } = IPAddress.Parse("239.255.50.10");
    public int ReceivePort { get; init; } = 5010;
    public TimeSpan InactivityTimeout { get; init; } = TimeSpan.FromSeconds(3);
    public TimeSpan MonitorInterval { get; init; } = TimeSpan.FromSeconds(1);
}
