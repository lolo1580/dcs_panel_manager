namespace DCSPanel.Hardware.Models;

public readonly record struct DeviceId(string Value)
{
    public override string ToString() => Value;
}

public enum DeviceType
{
    Unknown,
    LogitechPz55,
    LogitechPz70,
    StreamDeck
}

public enum DeviceConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Faulted
}

public sealed record DeviceDescriptor(
    DeviceId Id,
    DeviceType Type,
    string Model,
    int VendorId,
    int ProductId,
    string InstancePath,
    string? SerialNumber,
    DeviceConnectionState State,
    DateTimeOffset ConnectedAt);

public sealed record Control(string Id, string DisplayName, string Kind);

public enum InputEventKind
{
    Pressed,
    Released,
    Clockwise,
    CounterClockwise,
    ValueChanged
}

public sealed record InputEvent(
    DeviceId DeviceId,
    DeviceType DeviceType,
    string ControlId,
    InputEventKind Kind,
    bool IsActive,
    DateTimeOffset Timestamp);

public sealed record Output(string Id, object? Value);

public sealed record RawInputReport(
    DeviceId DeviceId,
    DeviceType DeviceType,
    ReadOnlyMemory<byte> Data,
    DateTimeOffset Timestamp);
