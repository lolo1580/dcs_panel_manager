using DCSPanel.Profiles.Models;

namespace DCSPanel.App.ViewModels;

public sealed class ProfileViewModel(AircraftProfile profile)
{
    public AircraftProfile Profile { get; } = profile;
    public string Id => Profile.Id;
    public string DisplayName => Profile.DisplayName;
    public string Aircraft => Profile.AircraftModule ?? "All aircraft (fallback)";
    public int DeviceCount => Profile.Devices.Count;
    public int MappingCount => Profile.Devices.Sum(device => device.Mappings.Count);
    public int OutputBindingCount => Profile.Devices.Sum(device => device.OutputBindings?.Count ?? 0);
    public string DevicesSummary => $"{DeviceCount} device type(s) · {MappingCount} mapping(s) · {OutputBindingCount} output(s)";
    public string SyncStrategy => Profile.Devices.Count == 0
        ? "No device strategy"
        : string.Join(", ", Profile.Devices.Select(device => device.SwitchSync).Distinct());
    public bool IsSafe => Profile.Devices.All(device => device.SwitchSync == PersistentSwitchSyncStrategy.NoSync);
    public string SafetyLabel => IsSafe ? "NoSync" : "Review sync policy";
}
