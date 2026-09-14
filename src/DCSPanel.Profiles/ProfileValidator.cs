using DCSPanel.Profiles.Models;

namespace DCSPanel.Profiles;

public sealed class ProfileValidator
{
    public const int CurrentSchemaVersion = 1;
    private readonly int _supportedSchemaVersion;

    public ProfileValidator() : this(CurrentSchemaVersion)
    {
    }

    internal ProfileValidator(int supportedSchemaVersion)
    {
        _supportedSchemaVersion = supportedSchemaVersion;
    }

    public ProfileValidationResult Validate(AircraftProfile? profile)
    {
        var errors = new List<string>();
        if (profile is null)
        {
            errors.Add("The profile is empty.");
            return new ProfileValidationResult(false, errors);
        }

        if (profile.SchemaVersion != _supportedSchemaVersion)
        {
            errors.Add($"Unsupported schema version: {profile.SchemaVersion}.");
        }

        if (string.IsNullOrWhiteSpace(profile.Id))
        {
            errors.Add("The profile identifier is required.");
        }

        if (string.IsNullOrWhiteSpace(profile.DisplayName))
        {
            errors.Add("The profile display name is required.");
        }

        foreach (var device in profile.Devices)
        {
            if (string.IsNullOrWhiteSpace(device.DeviceType))
            {
                errors.Add("Every device must declare a type.");
            }

            foreach (var mapping in device.Mappings)
            {
                if (string.IsNullOrWhiteSpace(mapping.ControlId))
                {
                    errors.Add("Every mapping must target a physical control.");
                }

                if (mapping.Actions.Count == 0)
                {
                    errors.Add($"Mapping {mapping.ControlId} does not contain any actions.");
                }
            }
        }

        return new ProfileValidationResult(errors.Count == 0, errors);
    }
}
