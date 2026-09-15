using DCSPanel.Core.Mapping;
using DCSPanel.Profiles.Models;

namespace DCSPanel.Profiles;

public static class ProfileMappingEditor
{
    public static AircraftProfile Upsert(AircraftProfile profile, InputMapping mapping)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(mapping);

        var devices = profile.Devices.ToList();
        var deviceIndex = devices.FindIndex(device =>
            string.Equals(device.DeviceType, mapping.DeviceType, StringComparison.OrdinalIgnoreCase));
        var device = deviceIndex >= 0
            ? devices[deviceIndex]
            : new DeviceProfile(mapping.DeviceType, null, PersistentSwitchSyncStrategy.NoSync, []);
        var mappings = device.Mappings
            .Where(existing => !HasSameTrigger(existing, mapping))
            .Append(mapping)
            .OrderBy(existing => existing.ControlId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(existing => existing.Kind)
            .ThenBy(existing => existing.IsActive)
            .ToArray();
        var updatedDevice = device with { Mappings = mappings };

        if (deviceIndex >= 0)
        {
            devices[deviceIndex] = updatedDevice;
        }
        else
        {
            devices.Add(updatedDevice);
        }

        return profile with { Devices = devices };
    }

    public static AircraftProfile Remove(AircraftProfile profile, InputMapping mapping)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(mapping);

        return profile with
        {
            Devices = profile.Devices.Select(device =>
                string.Equals(device.DeviceType, mapping.DeviceType, StringComparison.OrdinalIgnoreCase)
                    ? device with { Mappings = device.Mappings.Where(existing => !HasSameTrigger(existing, mapping)).ToArray() }
                    : device).ToArray()
        };
    }

    public static string GetFileName(AircraftProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var source = profile.AircraftModule ?? profile.Id;
        var invalidCharacters = Path.GetInvalidFileNameChars().ToHashSet();
        var safeName = new string(source
            .Select(character => invalidCharacters.Contains(character) ? '_' : character)
            .ToArray());
        return $"{safeName}.json";
    }

    private static bool HasSameTrigger(InputMapping left, InputMapping right) =>
        string.Equals(left.DeviceType, right.DeviceType, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(left.ControlId, right.ControlId, StringComparison.OrdinalIgnoreCase) &&
        left.Kind == right.Kind &&
        left.IsActive == right.IsActive;
}
