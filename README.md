# DCS Panel Manager

DCS Panel Manager is a modern Windows application for detecting, monitoring, and
eventually configuring flight simulation panels for DCS World.

The first supported devices are:

- Logitech/Saitek Pro Flight Switch Panel **PZ55**;
- Logitech/Saitek Pro Flight Multi Panel **PZ70**.

The project uses C#, .NET 10, and Avalonia UI. DCS-BIOS will be the primary interface
with DCS World; this project does not reimplement DCS-BIOS.

> The project is currently at an early milestone. HID input works, but no commands are
> sent to DCS World or to panel LED/LCD outputs yet.

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

- **Dashboard**: DCS World, DCS-BIOS, device, and active profile status;
- **Devices**: detailed list of detected panels;
- **Live Monitor**: raw HID reports and decoded events in real time;
- **Profiles**, **Mappings**, **DCS-BIOS**, and **Settings**: placeholders for upcoming
  milestones.

## Requirements

- Windows 10 or Windows 11;
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0);
- a PZ55 or PZ70 for hardware features;
- DCS World and DCS-BIOS are not required for the current milestone.

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

Current reference status: **0 warnings, 0 errors, 9 passing tests**.

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
|-- DCSPanel.DCSBIOS/            DCS-BIOS boundary, currently inactive
|-- DCSPanel.Profiles/           JSON profiles, persistence, and validation
`-- DCSPanel.App/                Avalonia UI and dependency composition

tests/
|-- DCSPanel.Core.Tests/
`-- DCSPanel.Hardware.Tests/

tools/
`-- DCSPanel.Hardware.Probe/
```

Core does not depend on Logitech, HID, or DCS-BIOS. Logitech protocol details remain
confined to `DCSPanel.Hardware.Logitech`.

Additional documentation:

- [Architecture decisions](docs/architecture.md)
- [HID protocol research](docs/hid-research.md)
- [Version history](CHANGELOG.md)

## Main dependencies

- **Avalonia 12**: the requested cross-platform user interface framework;
- **HidSharp**: raw HID device and report access;
- **Microsoft.Extensions.DependencyInjection**: modular dependency composition;
- **Microsoft.Extensions.Logging**: structured logging;
- **xUnit**: unit testing.

Core, Hardware, Profiles, and DCSBIOS avoid unnecessary external dependencies.

## Roadmap

### Milestone 2 - Read-only DCS-BIOS

- connection and disconnection detection;
- active aircraft detection;
- control metadata import;
- received DCS-BIOS data in Live Monitor.

### Milestone 3 - Mappings and commands

- profile and mapping editor;
- controlled DCS-BIOS command transmission;
- initial F-16C and F/A-18C profiles;
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

The current version sends no DCS-BIOS commands and writes no LED or LCD data. Loading a
profile does not automatically send physical switch positions to DCS.

## License

This project is distributed under the [MIT License](LICENSE).
