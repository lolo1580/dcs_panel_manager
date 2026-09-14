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
            errors.Add("Le profil est vide.");
            return new ProfileValidationResult(false, errors);
        }

        if (profile.SchemaVersion != _supportedSchemaVersion)
        {
            errors.Add($"Version de schéma non supportée : {profile.SchemaVersion}.");
        }

        if (string.IsNullOrWhiteSpace(profile.Id))
        {
            errors.Add("L'identifiant du profil est requis.");
        }

        if (string.IsNullOrWhiteSpace(profile.DisplayName))
        {
            errors.Add("Le nom du profil est requis.");
        }

        foreach (var device in profile.Devices)
        {
            if (string.IsNullOrWhiteSpace(device.DeviceType))
            {
                errors.Add("Chaque périphérique doit déclarer un type.");
            }

            foreach (var mapping in device.Mappings)
            {
                if (string.IsNullOrWhiteSpace(mapping.ControlId))
                {
                    errors.Add("Chaque mapping doit cibler un contrôle physique.");
                }

                if (mapping.Actions.Count == 0)
                {
                    errors.Add($"Le mapping {mapping.ControlId} ne contient aucune action.");
                }
            }
        }

        return new ProfileValidationResult(errors.Count == 0, errors);
    }
}
