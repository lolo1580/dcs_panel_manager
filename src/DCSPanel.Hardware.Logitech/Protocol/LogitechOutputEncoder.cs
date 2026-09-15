using DCSPanel.Hardware.Models;
using System.Globalization;

namespace DCSPanel.Hardware.Logitech.Protocol;

public static class LogitechOutputEncoder
{
    public static IReadOnlyList<byte[]> Encode(DeviceType deviceType, Output output)
    {
        ArgumentNullException.ThrowIfNull(output);

        return (deviceType, output.Id, output.Value) switch
        {
            (DeviceType.LogitechPz55, PanelOutputIds.Pz55GearLights, Pz55GearLights lights) =>
                [new byte[] { 0, 0 }, new byte[] { 0, EncodePz55Lights(lights) }],
            (DeviceType.LogitechPz70, PanelOutputIds.Pz70Panel, Pz70PanelOutput panel) =>
                [EncodePz70Panel(panel)],
            _ => throw new ArgumentException(
                $"Output '{output.Id}' is not supported by {deviceType}.", nameof(output))
        };
    }

    public static byte EncodePz55Lights(Pz55GearLights lights) =>
        (byte)(EncodePz55Light(lights.Upper, 0x01, 0x08) |
               EncodePz55Light(lights.Left, 0x02, 0x10) |
               EncodePz55Light(lights.Right, 0x04, 0x20));

    public static byte[] EncodePz70Panel(Pz70PanelOutput panel)
    {
        ArgumentNullException.ThrowIfNull(panel);

        var report = new byte[12];
        report[0] = 0;
        Array.Fill(report, (byte)0xFF, 1, 10);
        report[11] = (byte)panel.Lights;

        if (panel.UpperDisplay is int upper)
        {
            WriteDisplay(report, Math.Clamp(Math.Abs((long)upper), 0, 99999).ToString(CultureInfo.InvariantCulture), 5, false);
        }

        if (panel.LowerDisplay is int lower)
        {
            WriteDisplay(report, Math.Clamp((long)lower, -9999, 99999).ToString(CultureInfo.InvariantCulture), 10, true);
        }

        return report;
    }

    private static int EncodePz55Light(Pz55GearLightColor color, int green, int red) => color switch
    {
        Pz55GearLightColor.Off => 0,
        Pz55GearLightColor.Green => green,
        Pz55GearLightColor.Red => red,
        Pz55GearLightColor.Yellow => green | red,
        _ => throw new ArgumentOutOfRangeException(nameof(color), color, "Unsupported PZ55 LED color.")
    };

    private static void WriteDisplay(byte[] report, string value, int position, bool supportsMinus)
    {
        foreach (var character in value.Reverse())
        {
            report[position--] = character == '-' && supportsMinus ? (byte)0xEE : (byte)character;
        }
    }
}
