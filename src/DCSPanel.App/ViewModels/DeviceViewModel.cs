using DCSPanel.Hardware.Models;
using System.Globalization;

namespace DCSPanel.App.ViewModels;

public sealed class DeviceViewModel(DeviceDescriptor descriptor)
{
    public DeviceId Id { get; } = descriptor.Id;
    public string Model { get; } = descriptor.Model;
    public string VidPid { get; } = $"VID {descriptor.VendorId:X4} - PID {descriptor.ProductId:X4}";
    public string InstancePath { get; } = descriptor.InstancePath;
    public string SerialNumber { get; } = descriptor.SerialNumber ?? "Not provided";
    public string State { get; } = descriptor.State.ToString();
    public string ConnectedAt { get; } = descriptor.ConnectedAt.LocalDateTime.ToString("dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture);
}
