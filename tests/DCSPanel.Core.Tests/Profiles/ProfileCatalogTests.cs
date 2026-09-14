using DCSPanel.Profiles;
using DCSPanel.Profiles.Models;

namespace DCSPanel.Core.Tests.Profiles;

public sealed class ProfileCatalogTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"dcs-profile-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task LoadDirectoryAsyncLoadsValidProfilesAndReportsInvalidFiles()
    {
        Directory.CreateDirectory(_directory);
        await File.WriteAllTextAsync(
            Path.Combine(_directory, "generic.json"),
            """
            {
              "schemaVersion": 1,
              "id": "generic",
              "displayName": "Generic",
              "aircraftModule": null,
              "devices": []
            }
            """);
        await File.WriteAllTextAsync(Path.Combine(_directory, "invalid.json"), "{invalid");
        var catalog = new ProfileCatalog(new JsonProfileRepository(new ProfileValidator()));

        var result = await catalog.LoadDirectoryAsync(_directory);

        Assert.Equal("generic", Assert.Single(result.Profiles).Id);
        Assert.Equal("invalid.json", Path.GetFileName(Assert.Single(result.Errors).Path));
    }

    [Fact]
    public void SelectForAircraftUsesExactAircraftThenGenericFallback()
    {
        var generic = new AircraftProfile(1, "generic", "Generic", null, []);
        var f16 = new AircraftProfile(1, "f16", "F-16C", "F-16C_50", []);

        Assert.Same(f16, ProfileCatalog.SelectForAircraft([generic, f16], "F-16C_50"));
        Assert.Same(generic, ProfileCatalog.SelectForAircraft([generic, f16], "FA-18C_hornet"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }
}
