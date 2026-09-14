using DCSPanel.Hardware.Models;

namespace DCSPanel.Hardware.Abstractions;

public interface IHardwareService : IAsyncDisposable
{
    event EventHandler<HardwareEvent>? HardwareEventOccurred;

    IReadOnlyList<DeviceDescriptor> Devices { get; }

    Task StartAsync(CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);
}
