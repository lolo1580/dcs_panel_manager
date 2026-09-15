using DCSPanel.Core.Mapping;
using DCSPanel.Profiles;
using DCSPanel.Profiles.Models;

namespace DCSPanel.Core.Tests.Profiles;

public sealed class ProfileMappingEditorTests
{
    [Fact]
    public void UpsertAddsAndReplacesOnePhysicalTrigger()
    {
        var profile = StarterProfileFactory.Create("F-16C_50");
        var first = Mapping("1", true);
        var replacement = Mapping("2", true);

        var updated = ProfileMappingEditor.Upsert(ProfileMappingEditor.Upsert(profile, first), replacement);

        var mapping = Assert.Single(updated.Devices
            .Single(device => device.DeviceType == "LogitechPz55").Mappings);
        Assert.Equal("2", Assert.Single(mapping.Actions).Argument);
    }

    [Fact]
    public void RemoveKeepsOppositeSwitchState()
    {
        var profile = StarterProfileFactory.Create("F-16C_50");
        profile = ProfileMappingEditor.Upsert(profile, Mapping("1", true));
        profile = ProfileMappingEditor.Upsert(profile, Mapping("0", false));

        var updated = ProfileMappingEditor.Remove(profile, Mapping("1", true));

        var mapping = Assert.Single(updated.Devices
            .Single(device => device.DeviceType == "LogitechPz55").Mappings);
        Assert.False(mapping.IsActive);
    }

    [Fact]
    public void GetFileNameReplacesInvalidAircraftCharacters()
    {
        var profile = StarterProfileFactory.Create("F-16C:Block/50");

        var fileName = ProfileMappingEditor.GetFileName(profile);

        Assert.DoesNotContain(':', fileName);
        Assert.DoesNotContain('/', fileName);
        Assert.EndsWith(".json", fileName);
    }

    private static InputMapping Mapping(string argument, bool isActive) =>
        new("LogitechPz55", "MASTER_BAT", PhysicalInputKind.Switch,
            [new ActionDefinition("DCS-BIOS", "MAIN_PWR_SW", argument)], isActive);
}
