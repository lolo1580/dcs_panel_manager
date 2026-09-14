using DCSPanel.Hardware.Logitech.Protocol;
using DCSPanel.Hardware.Models;

namespace DCSPanel.Hardware.Tests.Logitech;

public sealed class LogitechInputDecoderTests
{
    private static readonly DeviceId TestDevice = new("test-device");

    [Fact]
    public void DecodeChangesDecodesPz55SwitchOnAndOff()
    {
        var on = LogitechInputDecoder.DecodeChanges(
            TestDevice, DeviceType.LogitechPz55, [0, 0, 0], [1, 0, 0], DateTimeOffset.UtcNow);
        var off = LogitechInputDecoder.DecodeChanges(
            TestDevice, DeviceType.LogitechPz55, [1, 0, 0], [0, 0, 0], DateTimeOffset.UtcNow);

        Assert.Equal("MASTER_BAT", Assert.Single(on).ControlId);
        Assert.Equal(InputEventKind.Pressed, on[0].Kind);
        Assert.Equal(InputEventKind.Released, Assert.Single(off).Kind);
    }

    [Fact]
    public void DecodeChangesDecodesPz70RotaryDirections()
    {
        var clockwise = LogitechInputDecoder.DecodeChanges(
            TestDevice, DeviceType.LogitechPz70, [0, 0, 0], [0b0010_0000, 0, 0], DateTimeOffset.UtcNow);
        var counterClockwise = LogitechInputDecoder.DecodeChanges(
            TestDevice, DeviceType.LogitechPz70, [0, 0, 0], [0b0100_0000, 0, 0], DateTimeOffset.UtcNow);

        Assert.Equal(InputEventKind.Clockwise, Assert.Single(clockwise).Kind);
        Assert.Equal(InputEventKind.CounterClockwise, Assert.Single(counterClockwise).Kind);
    }

    [Fact]
    public void DecodeChangesIgnoresUnchangedReport()
    {
        var result = LogitechInputDecoder.DecodeChanges(
            TestDevice, DeviceType.LogitechPz55, [1, 2, 4], [1, 2, 4], DateTimeOffset.UtcNow);

        Assert.Empty(result);
    }

    [Fact]
    public void DecodeChangesIgnoresEncoderFallingEdge()
    {
        var result = LogitechInputDecoder.DecodeChanges(
            TestDevice, DeviceType.LogitechPz70, [0b0010_0000, 0, 0], [0, 0, 0], DateTimeOffset.UtcNow);

        Assert.Empty(result);
    }
}
