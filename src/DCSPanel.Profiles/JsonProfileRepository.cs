using System.Text.Json;
using System.Text.Json.Serialization;
using DCSPanel.Profiles.Models;

namespace DCSPanel.Profiles;

public sealed class JsonProfileRepository(ProfileValidator validator)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<AircraftProfile> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(path);
        var profile = await JsonSerializer.DeserializeAsync<AircraftProfile>(stream, JsonOptions, cancellationToken)
            .ConfigureAwait(false);
        var validation = validator.Validate(profile);
        if (!validation.IsValid)
        {
            throw new InvalidDataException(string.Join(Environment.NewLine, validation.Errors));
        }

        return profile!;
    }

    public async Task SaveAsync(AircraftProfile profile, string path, CancellationToken cancellationToken = default)
    {
        var validation = validator.Validate(profile);
        if (!validation.IsValid)
        {
            throw new InvalidDataException(string.Join(Environment.NewLine, validation.Errors));
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        Directory.CreateDirectory(directory!);
        var temporaryPath = Path.Combine(directory!, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        try
        {
            await using (var stream = File.Create(temporaryPath))
            {
                await JsonSerializer.SerializeAsync(stream, profile, JsonOptions, cancellationToken).ConfigureAwait(false);
            }

            File.Move(temporaryPath, path, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }
}
