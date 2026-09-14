using DCSPanel.Profiles.Models;

namespace DCSPanel.Profiles;

public static class StarterProfileFactory
{
    public static AircraftProfile Create(string aircraftModule)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aircraftModule);
        return new AircraftProfile(
            ProfileValidator.CurrentSchemaVersion,
            $"dcs-bios:{aircraftModule}",
            ToDisplayName(aircraftModule),
            aircraftModule,
            [
                new DeviceProfile("LogitechPz55", null, PersistentSwitchSyncStrategy.NoSync, []),
                new DeviceProfile("LogitechPz70", null, PersistentSwitchSyncStrategy.NoSync, [])
            ]);
    }

    public static string ToDisplayName(string aircraftModule)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aircraftModule);
        return aircraftModule.Replace('_', ' ');
    }
}
