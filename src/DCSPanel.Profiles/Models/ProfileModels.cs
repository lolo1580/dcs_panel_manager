using DCSPanel.Core.Mapping;

namespace DCSPanel.Profiles.Models;

public enum PersistentSwitchSyncStrategy
{
    NoSync,
    HardwareWins,
    SimulatorWins
}

public sealed record DeviceProfile(
    string DeviceType,
    string? DeviceId,
    PersistentSwitchSyncStrategy SwitchSync,
    IReadOnlyList<InputMapping> Mappings,
    IReadOnlyList<PanelOutputBinding>? OutputBindings = null);

public enum PanelOutputTarget
{
    Pz55UpperGear,
    Pz55LeftGear,
    Pz55RightGear,
    Pz70UpperDisplay,
    Pz70LowerDisplay,
    Pz70ApLight,
    Pz70HdgLight,
    Pz70NavLight,
    Pz70IasLight,
    Pz70AltLight,
    Pz70VsLight,
    Pz70AprLight,
    Pz70RevLight
}

public enum PanelOutputColor
{
    Green,
    Red,
    Yellow
}

public sealed record PanelOutputBinding(
    PanelOutputTarget Target,
    string ControlId,
    PanelOutputColor Color = PanelOutputColor.Green);

public sealed record AircraftProfile(
    int SchemaVersion,
    string Id,
    string DisplayName,
    string? AircraftModule,
    IReadOnlyList<DeviceProfile> Devices);

public sealed record ProfileValidationResult(bool IsValid, IReadOnlyList<string> Errors);
