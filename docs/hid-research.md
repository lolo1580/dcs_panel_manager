# PZ55 / PZ70 HID research

Milestone 1 relies on the source code of
[DCSFlightpanels](https://github.com/DCS-Skunkworks/DCSFlightpanels). It does not copy
that application's architecture or reimplement DCS-BIOS.

Verified facts used by this implementation:

- PZ55 constructor: VID `0x06A3`, PID `0x0D67`;
- PZ70 constructor: VID `0x06A3`, PID `0x0D06`;
- both panels expose three input bytes;
- each control maps to a bit documented in `SwitchPanelKey.cs` and
  `MultiPanelKnob.cs`;
- the complete HID path is used as the instance identity. A serial number supplements
  that identity when available. VID/PID alone cannot distinguish two devices of the
  same model.

Primary sources:

- [SwitchPanelPZ55.cs](https://github.com/DCS-Skunkworks/DCSFlightpanels/blob/main/src/NonVisuals/Panels/Saitek/Panels/SwitchPanelPZ55.cs)
- [SwitchPanelKey.cs](https://github.com/DCS-Skunkworks/DCSFlightpanels/blob/main/src/NonVisuals/Panels/Saitek/Switches/SwitchPanelKey.cs)
- [MultiPanelPZ70.cs](https://github.com/DCS-Skunkworks/DCSFlightpanels/blob/main/src/NonVisuals/Panels/Saitek/Panels/MultiPanelPZ70.cs)
- [MultiPanelKnob.cs](https://github.com/DCS-Skunkworks/DCSFlightpanels/blob/main/src/NonVisuals/Panels/Saitek/Switches/MultiPanelKnob.cs)
- [HidSharp](https://github.com/IntergatedCircuits/HidSharp)

The referenced sources also document LED/LCD output formats. Output is intentionally
not implemented yet: milestone 1 must not transmit data to the panels or DCS.
