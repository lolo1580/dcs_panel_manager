using System.Net;
using System.Net.Sockets;
using System.Text;
using DCSPanel.Core.Events;
using DCSPanel.DCSBIOS.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;

namespace DCSPanel.DCSBIOS.Tests;

public sealed class UdpDcsBiosClientTests
{
    [Fact]
    public async Task ReceivePublishesDetectedAircraftFromDcsBiosMetadata()
    {
        var port = ReserveUdpPort();
        var options = new DcsBiosOptions
        {
            ReceivePort = port,
            MonitorInterval = TimeSpan.FromMilliseconds(50),
            InactivityTimeout = TimeSpan.FromSeconds(5)
        };
        await using var client = new UdpDcsBiosClient(
            NullLogger<UdpDcsBiosClient>.Instance,
            new ActivityHub(),
            options);
        var detected = new TaskCompletionSource<DcsBiosStateChanged>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        client.StateChanged += (_, state) =>
        {
            if (state.Aircraft == "F-16C_50")
            {
                detected.TrySetResult(state);
            }
        };

        await client.ConnectAsync();
        using var sender = new UdpClient(AddressFamily.InterNetwork);
        var packet = CreateAircraftFrame("F-16C_50");
        await sender.SendAsync(packet, new IPEndPoint(IPAddress.Loopback, port));

        var state = await detected.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Equal("F-16C_50", client.Aircraft);
        Assert.Equal("F-16C_50", state.Aircraft);
    }

    private static int ReserveUdpPort()
    {
        using var listener = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        return ((IPEndPoint)listener.Client.LocalEndPoint!).Port;
    }

    private static byte[] CreateAircraftFrame(string aircraft)
    {
        const int aircraftNameLength = 24;
        var name = new byte[aircraftNameLength];
        Encoding.ASCII.GetBytes(aircraft).CopyTo(name, 0);
        return
        [
            0x55, 0x55, 0x55, 0x55,
            0x00, 0x00,
            aircraftNameLength, 0x00,
            .. name,
            0x55, 0x55, 0x55, 0x55
        ];
    }
}
