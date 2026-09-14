using DCSPanel.Hardware.Models;

namespace DCSPanel.Hardware.Logitech;

public static class LogitechDeviceDefinitions
{
    public const int SaitekVendorId = 0x06A3;
    public const int Pz55ProductId = 0x0D67;
    public const int Pz70ProductId = 0x0D06;

    public static bool TryIdentify(int vendorId, int productId, out DeviceType type, out string model)
    {
        type = DeviceType.Unknown;
        model = "Unknown HID device";

        if (vendorId != SaitekVendorId)
        {
            return false;
        }

        (type, model) = productId switch
        {
            Pz55ProductId => (DeviceType.LogitechPz55, "Logitech/Saitek Pro Flight Switch Panel (PZ55)"),
            Pz70ProductId => (DeviceType.LogitechPz70, "Logitech/Saitek Pro Flight Multi Panel (PZ70)"),
            _ => (DeviceType.Unknown, "Unknown HID device")
        };

        return type != DeviceType.Unknown;
    }
}
