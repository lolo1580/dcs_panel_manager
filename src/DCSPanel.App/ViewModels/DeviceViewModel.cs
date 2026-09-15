using DCSPanel.Hardware.Models;
using System.Globalization;

namespace DCSPanel.App.ViewModels;

public sealed class DeviceViewModel(DeviceDescriptor descriptor, Func<Task> testOutput)
{
    public DeviceId Id { get; } = descriptor.Id;
    public DeviceType Type { get; } = descriptor.Type;
    public string Model { get; } = descriptor.Model;
    public string VidPid { get; } = $"VID {descriptor.VendorId:X4} - PID {descriptor.ProductId:X4}";
    public string InstancePath { get; } = descriptor.InstancePath;
    public string SerialNumber { get; } = descriptor.SerialNumber ?? "Not provided";
    public string State { get; } = descriptor.State.ToString();
    public string ConnectedAt { get; } = descriptor.ConnectedAt.LocalDateTime.ToString("dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture);
    public bool SupportsOutputTest { get; } = descriptor.Type is DeviceType.LogitechPz55 or DeviceType.LogitechPz70;
    public string OutputTestLabel => descriptor.Type == DeviceType.LogitechPz55 ? "Test gear LEDs" : "Test displays and LEDs";
    public AsyncRelayCommand TestOutputCommand { get; } = new(testOutput);
}
