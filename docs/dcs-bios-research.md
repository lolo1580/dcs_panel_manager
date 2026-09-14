# DCS-BIOS Protocol Research

This document records the sources and decisions used for the milestone 2 read-only
integration. DCS Panel Manager consumes DCS-BIOS; it does not reimplement the DCS
export script.

## Authoritative sources

- [DCS-BIOS Developer Guide](https://github.com/DCS-Skunkworks/dcs-bios/blob/main/Scripts/DCS-BIOS/doc/developerguide.adoc)
- [Default BIOSConfig.lua](https://github.com/DCS-Skunkworks/dcs-bios/blob/main/Scripts/DCS-BIOS/BIOSConfig.lua)
- [MetadataStart.lua](https://github.com/DCS-Skunkworks/dcs-bios/blob/main/Scripts/DCS-BIOS/lib/modules/common_modules/MetadataStart.lua)
- [Protocol.lua metadata generation](https://github.com/DCS-Skunkworks/dcs-bios/blob/main/Scripts/DCS-BIOS/lib/Protocol.lua)

The maintained DCS-Skunkworks repository is used because it is the continuation of the
original DCS-BIOS project and documents the current protocol and modules.

## Export transport

The default DCS-BIOS configuration sends binary cockpit state to multicast address
`239.255.50.10`, UDP port `5010`. Multiple local consumers are supported, so the socket
must enable address reuse before binding.

DCS-BIOS tries to publish 30 updates per second. DCS Panel Manager therefore samples
packet activity for Live Monitor while retaining the full packet count, avoiding an
unnecessarily noisy event list.

## Binary protocol

Every update begins with four `0x55` bytes. Each following write is encoded as:

```text
16-bit little-endian start address
16-bit little-endian byte count
payload bytes
```

The parser is incremental because a network buffer boundary is not a protocol boundary.
It ignores bytes until synchronization, accepts writes split across buffers, and resets
on invalid address ranges.

The export memory is a 16-bit address space. Writes are applied to a 64 KiB local state
buffer. DCS-BIOS strings may be updated in pieces, so string values are observed at the
next frame marker rather than during a partial write.

## Aircraft detection

`MetadataStart.lua` allocates its module at address `0x0000` and defines `_ACFT_NAME` as
a 24-byte null-terminated string. DCS-BIOS publishes it at mission start and clears it
at mission end. DCS Panel Manager uses only this protocol metadata identifier; it does
not contain an aircraft-name lookup table.

## Control metadata

DCS-BIOS writes its generated metadata beneath the DCS-BIOS script directory at:

```text
doc\json
```

`AircraftAliases.json` maps the active aircraft name to one or more module names such as
`CommonData` and the aircraft module. DCS Panel Manager resolves those aliases and merges
the corresponding JSON controls. Inputs and outputs retain their protocol fields,
including interface, address, mask, shift, maximum value, and string length.

## Read-only safety boundary

The official import protocol accepts commands on port `7778`, but milestone 2 does not
open that socket. `SendCommandAsync` always rejects requests. Command transmission will
only be introduced with mapping validation and the `NoSync` safety policy in a later
milestone.
