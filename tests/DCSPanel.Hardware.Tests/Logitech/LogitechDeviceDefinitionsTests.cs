using DCSPanel.Hardware.Logitech;
using DCSPanel.Hardware.Models;

namespace DCSPanel.Hardware.Tests.Logitech;

public sealed class LogitechDeviceDefinitionsTests
{
    [Theory]
    [InlineData(LogitechDeviceDefinitions.Pz55ProductId, DeviceType.LogitechPz55)]
    [InlineData(LogitechDeviceDefinitions.Pz70ProductId, DeviceType.LogitechPz70)]
    public void TryIdentifyRecognizesSupportedPanels(int productId, DeviceType expected)
    {
        var found = LogitechDeviceDefinitions.TryIdentify(
            LogitechDeviceDefinitions.SaitekVendorId,
            productId,
            out var actual,
            out var model);

        Assert.True(found);
        Assert.Equal(expected, actual);
        Assert.Contains(expected == DeviceType.LogitechPz55 ? "PZ55" : "PZ70", model);
    }
}
