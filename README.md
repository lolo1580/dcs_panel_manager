# DCS Panel Manager

DCS Panel Manager is a modern Windows application for detecting, monitoring, and
eventually configuring flight simulation panels for DCS World.

The first supported devices are:

- Logitech/Saitek Pro Flight Switch Panel **PZ55**;
- Logitech/Saitek Pro Flight Multi Panel **PZ70**.

The project uses C#, .NET 10, and Avalonia UI. DCS-BIOS will be the primary interface
with DCS World; this project does not reimplement DCS-BIOS.

> Version 0.3.1 adds read-only DCS-BIOS monitoring, automatic profile selection, a mapping
> browser, and a refined cockpit-inspired UI.
> No commands are sent to DCS World or
> to panel LED/LCD outputs yet.

## Current features

- automatic PZ55 and PZ70 detection;
- hot-plug connection, disconnection, and reconnection;
- support for multiple devices and multiple instances of the same model;
- individual identification using the complete HID path and serial number when available;
- display of VID, PID, instance path, and connection state;
- raw HID report capture;
- decoding for switches, buttons, selectors, and encoders;
- Live Monitor with Hardware, Mapping, DCS-BIOS, and Error filters;
- backend-independent mapping engine;
- versioned and validated JSON profiles;
- safe `NoSync` strategy by default;
- dependency injection and structured logging;
- command-line hardware diagnostic tool.
- read-only DCS-BIOS UDP multicast listener;
- DCS-BIOS connection and inactivity detection;
- active aircraft detection through the official `_ACFT_NAME` export;
- automatic import of aircraft control metadata from DCS-BIOS JSON files;
- DCS-BIOS packet count and sampled receive activity in Live Monitor.
- validated JSON profile catalog plus automatically generated starters for every aircraft
  and helicopter exposed by the installed DCS-BIOS metadata;
- automatic aircraft profile selection with Generic fallback;
- data-driven Profiles and Mappings pages.

## Hardware validation

HID input has been tested with real hardware on Windows:

| Panel | USB identifier | Status |
|---|---|---|
| PZ55 Switch Panel | `VID 06A3 / PID 0D67` | Detection, reports, and switches validated |
| PZ70 Multi Panel | `VID 06A3 / PID 0D06` | Detection, reports, and controls validated |

Testing confirmed PZ55 state changes and PZ70 selector positions. Streams remain open
while panels are idle and close cleanly when the application stops.

## User interface

The application currently contains these pages:

- **Dashboard**: system overview, live DCS World/DCS-BIOS status, connected panels, and
  active profile;
- **Devices**: detailed cards for every detected panel and instance;
- **Live Monitor**: structured and filterable event console for HID, mapping, DCS-BIOS,
  and error events;
- **DCS-BIOS**: connection, packet, aircraft metadata, and read-only safety status;
- **Profiles**: selectable catalog with aircraft, device, mapping, and safety details;
- **Mappings**: active-profile mapping browser;
- **Settings**: current safe configuration and upcoming preferences.

## Requirements

- Windows 10 or Windows 11;
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0);
- a PZ55 or PZ70 for hardware features;
- [DCS-BIOS](https://github.com/DCS-Skunkworks/dcs-bios) for simulator data. The
  application and hardware monitoring still start when it is absent.

DCS Panel Manager discovers metadata in the standard installation path:

```text
%USERPROFILE%\Saved Games\DCS*\Scripts\DCS-BIOS\doc\json
```

With DCS World running in a mission, the application listens to the official export
multicast group `239.255.50.10:5010`. The Dashboard changes to **Connected** after a
valid protocol frame is received and displays the active aircraft when available.

Check the installed SDK:

```powershell
dotnet --version
```

## Build and test

From PowerShell:

```powershell
git clone https://github.com/lolo1580/dcs_panel_manager.git
cd dcs_panel_manager

dotnet restore .\DCSPanelManager.sln
dotnet build .\DCSPanelManager.sln -c Release
dotnet test .\DCSPanelManager.sln -c Release --no-build
```

Current reference status: **0 warnings, 0 errors, 18 passing tests**.

## Run the application

```powershell
dotnet run --project .\src\DCSPanel.App\DCSPanel.App.csproj -c Release
```

Connect the panels, then open **Devices** or **Live Monitor**.

## Hardware diagnostics

The probe uses the same HID service as the desktop application. The final argument is
the test duration in seconds:

```powershell
dotnet run --project .\tools\DCSPanel.Hardware.Probe\DCSPanel.Hardware.Probe.csproj -c Release -- 20
```

Operate switches, buttons, and encoders during the test. A successful run reports both
connections, raw reports, decoded inputs, and `errors=0`.

If a panel cannot be opened, close Logitech Flight Panels, DCSFlightpanels, or any other
application that may be using the HID device exclusively.

## Architecture

```text
src/
|-- DCSPanel.Core/               Abstractions, events, and mapping engine
|-- DCSPanel.Hardware/           Generic hardware models and contracts
|-- DCSPanel.Hardware.Logitech/  HID enumeration and PZ55/PZ70 protocol
|-- DCSPanel.DCSBIOS/            Read-only UDP transport, parser, and metadata import
|-- DCSPanel.Profiles/           JSON profiles, persistence, and validation
`-- DCSPanel.App/                Avalonia UI and dependency composition

tests/
|-- DCSPanel.Core.Tests/
|-- DCSPanel.DCSBIOS.Tests/
`-- DCSPanel.Hardware.Tests/

tools/
`-- DCSPanel.Hardware.Probe/
```

Core does not depend on Logitech, HID, or DCS-BIOS. Logitech protocol details remain
confined to `DCSPanel.Hardware.Logitech`.

Additional documentation:

- [Architecture decisions](docs/architecture.md)
- [HID protocol research](docs/hid-research.md)
- [DCS-BIOS protocol research](docs/dcs-bios-research.md)
- [Version history](CHANGELOG.md)

## Main dependencies

- **Avalonia 12**: the requested cross-platform user interface framework;
- **HidSharp**: raw HID device and report access;
- **Microsoft.Extensions.DependencyInjection**: modular dependency composition;
- **Microsoft.Extensions.Logging**: structured logging;
- **xUnit**: unit testing.

Core, Hardware, Profiles, and DCSBIOS avoid unnecessary external dependencies.

## Roadmap

### Milestone 3 - Mappings and commands

- visual mapping editor;
- controlled DCS-BIOS command transmission;
- validated F-16C and F/A-18C mappings;
- `NoSync` retained as the safe default strategy.

### Milestone 4 - Hardware feedback

- PZ55 LEDs;
- PZ70 LCD and LEDs;
- DCS State -> Hardware Output feedback mappings.

### Later

- modifiers, layers, and multiple actions;
- keyboard backend;
- Stream Deck and additional device support.

## Operational safety

The current version opens only the DCS-BIOS export listener. It creates no command
socket, sends no DCS-BIOS commands, and writes no LED or LCD data. Loading a profile
does not automatically send physical switch positions to DCS.

## License

This project is distributed under the [MIT License](LICENSE).
