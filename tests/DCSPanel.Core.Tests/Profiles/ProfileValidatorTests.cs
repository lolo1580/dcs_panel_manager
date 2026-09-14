using DCSPanel.Profiles;
using DCSPanel.Profiles.Models;

namespace DCSPanel.Core.Tests.Profiles;

public sealed class ProfileValidatorTests
{
    [Fact]
    public void ValidateAcceptsSafeGenericProfile()
    {
        var profile = new AircraftProfile(
            ProfileValidator.CurrentSchemaVersion,
            "generic",
            "Generic",
            null,
            [new DeviceProfile("LogitechPz55", null, PersistentSwitchSyncStrategy.NoSync, [])]);

        var result = new ProfileValidator().Validate(profile);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Equal(PersistentSwitchSyncStrategy.NoSync, profile.Devices[0].SwitchSync);
    }

    [Fact]
    public void ValidateRejectsUnknownSchemaVersion()
    {
        var profile = new AircraftProfile(99, "generic", "Generic", null, []);

        var result = new ProfileValidator().Validate(profile);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("schema", StringComparison.OrdinalIgnoreCase));
    }
}
