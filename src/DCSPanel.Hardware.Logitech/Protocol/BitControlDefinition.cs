namespace DCSPanel.Hardware.Logitech.Protocol;

internal sealed record BitControlDefinition(int ByteIndex, byte Mask, string ControlId, bool IsEncoder = false, bool Clockwise = false);
