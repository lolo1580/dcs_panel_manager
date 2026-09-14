namespace DCSPanel.Hardware.Models;

public enum HardwareEventKind
{
    Connected,
    Disconnected,
    RawInput,
    Input,
    Error
}

public sealed record HardwareEvent(
    HardwareEventKind Kind,
    DeviceDescriptor Device,
    RawInputReport? RawReport = null,
    InputEvent? Input = null,
    Exception? Exception = null);
