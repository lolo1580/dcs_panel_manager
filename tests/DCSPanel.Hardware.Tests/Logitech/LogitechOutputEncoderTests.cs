using DCSPanel.Hardware.Logitech.Protocol;
using DCSPanel.Hardware.Models;

namespace DCSPanel.Hardware.Tests.Logitech;

public sealed class LogitechOutputEncoderTests
{
    [Fact]
    public void EncodePz55LightsBuildsExpectedFeatureReports()
    {
        var reports = LogitechOutputEncoder.Encode(
            DeviceType.LogitechPz55,
            new Output(PanelOutputIds.Pz55GearLights,
                new Pz55GearLights(Pz55GearLightColor.Yellow, Pz55GearLightColor.Green, Pz55GearLightColor.Red)));

        Assert.Collection(reports,
            report => Assert.Equal(new byte[] { 0x00, 0x00 }, report),
            report => Assert.Equal(new byte[] { 0x00, 0x2B }, report));
    }

    [Fact]
    public void EncodePz70PanelBuildsDisplaysAndAutopilotLights()
    {
        var reports = LogitechOutputEncoder.Encode(
            DeviceType.LogitechPz70,
            new Output(PanelOutputIds.Pz70Panel,
                new Pz70PanelOutput(12345, -6789, Pz70AutopilotLights.Ap | Pz70AutopilotLights.Nav)));

        var report = Assert.Single(reports);
        Assert.Equal(new byte[]
        {
            0x00, (byte)'1', (byte)'2', (byte)'3', (byte)'4', (byte)'5', 0xEE,
            (byte)'6', (byte)'7', (byte)'8', (byte)'9', 0x05, 0xFF
        }, report);
    }

    [Fact]
    public void EncodePz70PanelClampsDisplayRanges()
    {
        var report = LogitechOutputEncoder.Encode(
            DeviceType.LogitechPz70,
            new Output(PanelOutputIds.Pz70Panel, new Pz70PanelOutput(int.MinValue, int.MinValue))).Single();

        Assert.Equal(new byte[] { (byte)'9', (byte)'9', (byte)'9', (byte)'9', (byte)'9' }, report[1..6]);
        Assert.Equal(new byte[] { 0xEE, (byte)'9', (byte)'9', (byte)'9', (byte)'9' }, report[6..11]);
    }
}
