# Architecture

Dependencies point toward abstractions and never toward the user interface:

```text
DCSPanel.App
 |-- DCSPanel.Core
 |-- DCSPanel.Hardware --> DCSPanel.Core
 |-- DCSPanel.Hardware.Logitech --> Hardware + Core
 |-- DCSPanel.DCSBIOS --> Core
 `-- DCSPanel.Profiles --> Core
```

Key decisions:

1. `DCSPanel.Core` has no knowledge of Logitech, HID, or DCS-BIOS.
2. `IHardwareService` represents enumeration, hot-plug behavior, and input streams.
3. The Logitech driver converts HID reports into generic events while also publishing
   raw reports for Live Monitor.
4. The mapping engine selects a backend by name. DCS-BIOS, keyboard input, or future
   backends can be added without modifying a hardware driver.
5. JSON profiles are versioned and validated. `NoSync` is explicitly selected by the
   generic profile.
6. The milestone 1 DCS-BIOS client is an inactive boundary: it opens no sockets and
   transmits no commands.
