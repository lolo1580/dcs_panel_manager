namespace DCSPanel.Hardware.Models;

public enum Pz55GearLightColor
{
    Off,
    Green,
    Red,
    Yellow
}

public sealed record Pz55GearLights(
    Pz55GearLightColor Upper,
    Pz55GearLightColor Left,
    Pz55GearLightColor Right);

[Flags]
public enum Pz70AutopilotLights : byte
{
    None = 0,
    Ap = 1 << 0,
    Hdg = 1 << 1,
    Nav = 1 << 2,
    Ias = 1 << 3,
    Alt = 1 << 4,
    Vs = 1 << 5,
    Apr = 1 << 6,
    Rev = 1 << 7,
    All = byte.MaxValue
}

public sealed record Pz70PanelOutput(
    int? UpperDisplay,
    int? LowerDisplay,
    Pz70AutopilotLights Lights = Pz70AutopilotLights.None);

public static class PanelOutputIds
{
    public const string Pz55GearLights = "pz55.gear-lights";
    public const string Pz70Panel = "pz70.panel";
}
