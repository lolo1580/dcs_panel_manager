# Changelog

All notable changes to DCS Panel Manager are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the
project uses [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Changed

- translated the user interface, logs, validation messages, comments, tests, and all
  repository documentation to English.

### Planned

- read-only DCS-BIOS connection;
- DCS World and active aircraft detection;
- DCS-BIOS metadata import;
- received DCS-BIOS data in Live Monitor.

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
