using DCSPanel.Profiles;
using DCSPanel.Profiles.Models;

namespace DCSPanel.Core.Tests.Profiles;

public sealed class StarterProfileFactoryTests
{
    [Fact]
    public void CreateBuildsSafePz55AndPz70Profile()
    {
        var profile = StarterProfileFactory.Create("UH-1H");

        Assert.Equal("UH-1H", profile.AircraftModule);
        Assert.Equal(2, profile.Devices.Count);
        Assert.Contains(profile.Devices, device => device.DeviceType == "LogitechPz55");
        Assert.Contains(profile.Devices, device => device.DeviceType == "LogitechPz70");
        Assert.All(profile.Devices, device =>
        {
            Assert.Equal(PersistentSwitchSyncStrategy.NoSync, device.SwitchSync);
            Assert.Empty(device.Mappings);
        });
    }
}
