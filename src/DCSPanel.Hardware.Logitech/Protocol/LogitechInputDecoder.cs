using DCSPanel.Hardware.Models;

namespace DCSPanel.Hardware.Logitech.Protocol;

public static class LogitechInputDecoder
{
    private static readonly Dictionary<DeviceType, IReadOnlyList<BitControlDefinition>> Definitions =
        new Dictionary<DeviceType, IReadOnlyList<BitControlDefinition>>
        {
            [DeviceType.LogitechPz55] = CreatePz55Definitions(),
            [DeviceType.LogitechPz70] = CreatePz70Definitions()
        };

    public static IReadOnlyList<InputEvent> DecodeChanges(
        DeviceId deviceId,
        DeviceType deviceType,
        ReadOnlySpan<byte> previous,
        ReadOnlySpan<byte> current,
        DateTimeOffset timestamp)
    {
        if (!Definitions.TryGetValue(deviceType, out var definitions) || current.Length < 3)
        {
            return [];
        }

        var result = new List<InputEvent>();
        foreach (var definition in definitions)
        {
            var wasActive = previous.Length >= 3 && (previous[definition.ByteIndex] & definition.Mask) != 0;
            var isActive = (current[definition.ByteIndex] & definition.Mask) != 0;
            if (wasActive == isActive)
            {
                continue;
            }

            // Encoders emit a pulse. The falling edge must not become a second
            // detent in the same direction.
            if (definition.IsEncoder && !isActive)
            {
                continue;
            }

            var kind = definition.IsEncoder
                ? definition.Clockwise ? InputEventKind.Clockwise : InputEventKind.CounterClockwise
                : isActive ? InputEventKind.Pressed : InputEventKind.Released;

            result.Add(new InputEvent(deviceId, deviceType, definition.ControlId, kind, isActive, timestamp));
        }

        return result;
    }

    private static IReadOnlyList<BitControlDefinition> CreatePz55Definitions() =>
    [
        Bit(0, 0, "MASTER_BAT"), Bit(0, 1, "MASTER_ALT"), Bit(0, 2, "AVIONICS_MASTER"),
        Bit(0, 3, "FUEL_PUMP"), Bit(0, 4, "DE_ICE"), Bit(0, 5, "PITOT_HEAT"),
        Bit(0, 6, "COWL"), Bit(0, 7, "LIGHTS_PANEL"), Bit(1, 0, "LIGHTS_BEACON"),
        Bit(1, 1, "LIGHTS_NAV"), Bit(1, 2, "LIGHTS_STROBE"), Bit(1, 3, "LIGHTS_TAXI"),
        Bit(1, 4, "LIGHTS_LANDING"), Bit(1, 5, "ENGINE_OFF"), Bit(1, 6, "ENGINE_RIGHT"),
        Bit(1, 7, "ENGINE_LEFT"), Bit(2, 0, "ENGINE_BOTH"), Bit(2, 1, "ENGINE_START"),
        Bit(2, 2, "GEAR_UP"), Bit(2, 3, "GEAR_DOWN")
    ];

    private static IReadOnlyList<BitControlDefinition> CreatePz70Definitions() =>
    [
        Bit(0, 0, "KNOB_ALT"), Bit(0, 1, "KNOB_VS"), Bit(0, 2, "KNOB_IAS"),
        Bit(0, 3, "KNOB_HDG"), Bit(0, 4, "KNOB_CRS"), Encoder(0, 5, "LCD_WHEEL", true),
        Encoder(0, 6, "LCD_WHEEL", false), Bit(0, 7, "AP_BUTTON"), Bit(1, 0, "HDG_BUTTON"),
        Bit(1, 1, "NAV_BUTTON"), Bit(1, 2, "IAS_BUTTON"), Bit(1, 3, "ALT_BUTTON"),
        Bit(1, 4, "VS_BUTTON"), Bit(1, 5, "APR_BUTTON"), Bit(1, 6, "REV_BUTTON"),
        Bit(1, 7, "AUTO_THROTTLE"), Bit(2, 0, "FLAPS_UP"), Bit(2, 1, "FLAPS_DOWN"),
        Encoder(2, 2, "PITCH_TRIM", false), Encoder(2, 3, "PITCH_TRIM", true)
    ];

    private static BitControlDefinition Bit(int byteIndex, int bit, string id) =>
        new(byteIndex, (byte)(1 << bit), id);

    private static BitControlDefinition Encoder(int byteIndex, int bit, string id, bool clockwise) =>
        new(byteIndex, (byte)(1 << bit), id, true, clockwise);
}
