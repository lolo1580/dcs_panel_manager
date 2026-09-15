using DCSPanel.Core.Mapping;
using DCSPanel.Profiles;

namespace DCSPanel.Core.Tests.Profiles;

public sealed class JsonProfileRepositoryTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"dcs-profile-save-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task SaveAsyncPersistsActiveSwitchStateAndLeavesNoTemporaryFile()
    {
        var repository = new JsonProfileRepository(new ProfileValidator());
        var profile = ProfileMappingEditor.Upsert(
            StarterProfileFactory.Create("F-16C_50"),
            new InputMapping(
                "LogitechPz55",
                "MASTER_BAT",
                PhysicalInputKind.Switch,
                [new ActionDefinition("DCS-BIOS", "MAIN_PWR_SW", "1")],
                true));
        var path = Path.Combine(_directory, "F-16C_50.json");

        await repository.SaveAsync(profile, path);
        var loaded = await repository.LoadAsync(path);

        var mapping = Assert.Single(loaded.Devices.Single(device => device.DeviceType == "LogitechPz55").Mappings);
        Assert.True(mapping.IsActive);
        Assert.Empty(Directory.EnumerateFiles(_directory, "*.tmp"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }
}
