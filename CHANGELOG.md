# Changelog

All notable changes to DCS Panel Manager are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the
project uses [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Planned

- profile and mapping editor;
- controlled DCS-BIOS command transmission;
- PZ55 LED and PZ70 LCD/LED output.

## [0.3.1] - 2026-09-14

Complete DCS-BIOS aircraft profile discovery.

### Added

- automatic starter profile generation for every non-empty aircraft alias exposed by
  the installed DCS-BIOS version;
- live profile count sourced from the local DCS-BIOS metadata;
- tests for aircraft alias discovery and generated profile safety.

### Changed

- explicit JSON profiles now override generated profiles for the same aircraft;
- Profiles now lists airplanes, helicopters, community modules, and other aircraft
  supported by the installed DCS-BIOS metadata;
- bumped the project version to `0.3.1`.

### Safety

- every generated profile includes PZ55 and PZ70 with `NoSync` and no actions;
- generating or selecting a profile does not send commands to DCS World.

## [0.3.0] - 2026-09-14

Profile catalog and mapping browser milestone.

### Added

- automatic loading and validation of every JSON profile shipped with the application;
- invalid profile reporting in structured logs and Live Monitor;
- automatic profile selection from the aircraft reported by DCS-BIOS;
- safe fallback to the Generic profile for unsupported or disconnected aircraft;
- selectable profile catalog with aircraft, device, mapping, and synchronization details;
- mapping browser showing physical input, input type, backend, command, and argument;
- safe empty starter profiles for the F-16C Viper and F/A-18C Hornet;
- profile catalog and aircraft selection tests.

### Safety

- all starter profiles use `NoSync`;
- selecting or automatically changing a profile never transmits switch states;
- starter aircraft profiles intentionally contain no commands until DCS-BIOS metadata
  can be validated against a local installation.

### Changed

- replaced the Profiles and Mappings previews with live data-driven pages;
- bumped the project version to `0.3.0`.

## [0.2.1] - 2026-09-14

User interface refinement release.

### Changed

- redesigned the application shell with a consistent cockpit-inspired dark theme;
- added a clearer navigation rail with page identifiers and selected states;
- rebuilt the Dashboard around a system overview, live status cards, connected hardware,
  and DCS-BIOS activity;
- improved connection indicators with live status colors;
- redesigned Devices with clearer identity, connection, serial, and instance information;
- expanded the Profiles, Mappings, and Settings placeholders into informative workflow
  previews;
- redesigned the DCS-BIOS page around stream, packet, endpoint, and metadata panels;
- reformatted Live Monitor as a structured event console with a fixed header and filters;
- improved spacing, typography, contrast, and visual consistency throughout the app;
- bumped the project version to `0.2.1`.

## [0.2.0] - 2026-09-14

Read-only DCS-BIOS integration milestone.

### Added

- UDP multicast listener for the official DCS-BIOS export stream on
  `239.255.50.10:5010`;
- incremental binary protocol parser supporting split buffers and frame synchronization;
- in-memory DCS-BIOS address space with frame-consistent string reads;
- DCS-BIOS connection, inactivity, and fault detection;
- active aircraft detection from `_ACFT_NAME` at the official metadata address;
- automatic metadata discovery under `Saved Games\DCS*\Scripts\DCS-BIOS\doc\json`;
- aircraft alias resolution and JSON control metadata import;
- DCS-BIOS dashboard status, packet counters, metadata state, and Live Monitor events;
- five tests for protocol parsing, memory writes, and metadata import;
- DCS-BIOS protocol research and architecture documentation.

### Changed

- replaced the inactive milestone client with a real read-only listener;
- translated the user interface, logs, validation messages, comments, tests, and all
  repository documentation to English;
- bumped the project version to `0.2.0`.

### Safety

- command transmission remains disabled and no command socket is created;
- hardware monitoring continues when DCS-BIOS is absent or the listener cannot start.

### Environment validation

- application starts successfully with PZ55/PZ70 connected;
- no local DCS-BIOS installation was present during validation, so live simulator data
  and aircraft detection remain to be validated with DCS World running.

## [0.1.0] - 2026-09-14

Initial functional milestone focused on panel detection and input capture.

### Added

- .NET 10 solution with Core, Hardware, Logitech, DCSBIOS, Profiles, and App modules;
- Avalonia interface with Dashboard, Devices, and Live Monitor;
- navigation placeholders for Profiles, Mappings, DCS-BIOS, and Settings;
- PZ55 detection using `VID 06A3 / PID 0D67`;
- PZ70 detection using `VID 06A3 / PID 0D06`;
- individual identity based on the HID path and available serial number;
- hot-plug connection and disconnection monitoring;
- raw HID report capture and display;
- PZ55 switch and selector decoding;
- PZ70 button, selector, and encoder decoding;
- generic connection, disconnection, and input events;
- Hardware, Mapping, DCS-BIOS, and Error filters in Live Monitor;
- backend-independent mapping engine;
- JSON profile models with schema versioning;
- `NoSync`, `HardwareWins`, and `SimulatorWins` strategies;
- generic profile using `NoSync`;
- DCS-BIOS client and metadata abstractions;
- inactive DCS-BIOS client preventing transmission during this milestone;
- dependency injection and structured logging;
- unit tests for Core, profiles, and the Logitech protocol;
- `DCSPanel.Hardware.Probe` hardware diagnostic tool.

### Validated on real hardware

- simultaneous PZ55 and PZ70 detection on Windows;
- successful opening of both HID streams;
- PZ55 state change capture and decoding;
- PZ70 state capture and decoding;
- idle operation without read errors;
- clean HID stream shutdown.

### Fixed

- HidSharp read timeouts after three seconds of inactivity are no longer treated as
  device errors;
- normal HID stream closure is no longer logged as an error;
- an encoder pulse falling edge no longer generates a second detent;
- service shutdown correctly releases streams and reader tasks.

### Not implemented

- network connection to DCS-BIOS;
- command transmission to DCS World;
- PZ55 LED output;
- PZ70 LCD and LED output;
- graphical profile and mapping editors;
- keyboard backend and Stream Deck support.
