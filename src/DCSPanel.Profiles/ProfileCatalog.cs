using System.Text.Json;
using DCSPanel.Profiles.Models;

namespace DCSPanel.Profiles;

public sealed record ProfileCatalogError(string Path, string Message);

public sealed record ProfileCatalogResult(
    IReadOnlyList<AircraftProfile> Profiles,
    IReadOnlyList<ProfileCatalogError> Errors);

public sealed class ProfileCatalog(JsonProfileRepository repository)
{
    public async Task<ProfileCatalogResult> LoadDirectoryAsync(
        string directory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        if (!Directory.Exists(directory))
        {
            return new ProfileCatalogResult([], []);
        }

        var profiles = new List<AircraftProfile>();
        var errors = new List<ProfileCatalogError>();
        foreach (var path in Directory.EnumerateFiles(directory, "*.json", SearchOption.TopDirectoryOnly)
                     .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                profiles.Add(await repository.LoadAsync(path, cancellationToken).ConfigureAwait(false));
            }
            catch (Exception exception) when (exception is IOException or JsonException or InvalidDataException or UnauthorizedAccessException)
            {
                errors.Add(new ProfileCatalogError(path, exception.Message));
            }
        }

        return new ProfileCatalogResult(
            profiles.OrderBy(profile => profile.DisplayName, StringComparer.OrdinalIgnoreCase).ToArray(),
            errors);
    }

    public static AircraftProfile? SelectForAircraft(
        IEnumerable<AircraftProfile> profiles,
        string? aircraft)
    {
        ArgumentNullException.ThrowIfNull(profiles);
        var available = profiles.ToArray();
        if (!string.IsNullOrWhiteSpace(aircraft))
        {
            var aircraftProfile = available.FirstOrDefault(profile =>
                string.Equals(profile.AircraftModule, aircraft, StringComparison.OrdinalIgnoreCase));
            if (aircraftProfile is not null)
            {
                return aircraftProfile;
            }
        }

        return available.FirstOrDefault(profile => string.Equals(profile.Id, "generic", StringComparison.OrdinalIgnoreCase))
               ?? available.FirstOrDefault();
    }
}
