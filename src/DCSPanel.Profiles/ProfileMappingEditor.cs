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

    public static AircraftProfile UpsertOutput(AircraftProfile profile, PanelOutputBinding binding)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(binding);
        var deviceType = binding.Target.ToString().StartsWith("Pz55", StringComparison.Ordinal)
            ? "LogitechPz55"
            : "LogitechPz70";
        var devices = profile.Devices.ToList();
        var index = devices.FindIndex(device => string.Equals(device.DeviceType, deviceType, StringComparison.OrdinalIgnoreCase));
        var device = index >= 0
            ? devices[index]
            : new DeviceProfile(deviceType, null, PersistentSwitchSyncStrategy.NoSync, []);
        var bindings = (device.OutputBindings ?? [])
            .Where(existing => existing.Target != binding.Target)
            .Append(binding)
            .OrderBy(existing => existing.Target)
            .ToArray();
        var updated = device with { OutputBindings = bindings };
        if (index >= 0) devices[index] = updated; else devices.Add(updated);
        return profile with { Devices = devices };
    }

    public static AircraftProfile RemoveOutput(AircraftProfile profile, PanelOutputBinding binding) => profile with
    {
        Devices = profile.Devices.Select(device => device with
        {
            OutputBindings = (device.OutputBindings ?? []).Where(existing => !Equals(existing, binding)).ToArray()
        }).ToArray()
    };

    private static bool HasSameTrigger(InputMapping left, InputMapping right) =>
        string.Equals(left.DeviceType, right.DeviceType, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(left.ControlId, right.ControlId, StringComparison.OrdinalIgnoreCase) &&
        left.Kind == right.Kind &&
        left.IsActive == right.IsActive;
}
