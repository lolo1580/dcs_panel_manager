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
    IReadOnlyList<InputMapping> Mappings);

public sealed record AircraftProfile(
    int SchemaVersion,
    string Id,
    string DisplayName,
    string? AircraftModule,
    IReadOnlyList<DeviceProfile> Devices);

public sealed record ProfileValidationResult(bool IsValid, IReadOnlyList<string> Errors);
