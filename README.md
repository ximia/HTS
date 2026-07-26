# HTS Tuner (Mac-native)

A clean-room, cross-platform rewrite of the HTS "Snake Edition" Honda/Acura
OBD1 tuning tool, so it can run **natively on Apple Silicon Macs** instead of
only on Windows. Target hardware: **Moates Ostrich 2.0** real-time emulator +
**Moates HULOG** datalog cable, tuning a **P28** ECU (e.g. 2001 Acura Integra).

> Status: **early scaffold**. The hardware/serial layer and protocol are
> implemented and the app compiles and launches. It has **not** yet been tested
> against real hardware or a car — see "Testing & safety" before connecting it
> to an ECU.

## Why a rewrite instead of just running the .exe

The original `HTS 2.22 F2 Snake_Edition.exe` is a **.NET Framework 4.8 WinForms**
Windows program. Analysis of the binary showed:

| Original component | Windows dependency | Portable? |
| --- | --- | --- |
| HULOG datalogging | managed `System.IO.Ports.SerialPort` | ✅ yes |
| Ostrich emulator | managed `SerialPort` @ **312500 baud** | ✅ yes |
| `snake4hts32.dll` → `snake_search()` | 32-bit native DLL that finds the Ostrich COM port | ⚠️ replaced with managed port scanning |
| User interface | WinForms (Windows-only) | ❌ rebuilt in Avalonia |

The important finding: **there is no FTDI/D2XX native driver dependency** — the
app talks to both cables as ordinary serial ports. On macOS those cables
enumerate as `/dev/cu.usbserial-*` via FTDI's macOS VCP driver, so the same
serial logic works with no native code. That is what makes a native Mac port
practical.

## Project layout

```
src/
  HtsTuner.Core/            cross-platform library (no UI)
    Protocol/HondaFirmware.cs      command + response definitions
    Hardware/SerialDeviceScanner.cs  managed replacement for snake_search()
    Hardware/OstrichEmulator.cs      Ostrich handshake @ 312500 baud
    Hardware/HulogDatalogger.cs      live datalog stream reader
  HtsTuner.App/             Avalonia desktop UI (macOS/Windows/Linux)
```

## Build & run on a Mac

1. Install the [.NET SDK 8.0+](https://dotnet.microsoft.com/download).
2. Install the **FTDI VCP driver** for macOS so the cables appear as
   `/dev/cu.usbserial-*`.
3. From the repo root:
   ```bash
   dotnet build
   dotnet run --project src/HtsTuner.App
   ```
4. Click **Scan for hardware**, select the Ostrich's port, then
   **Handshake Ostrich** to confirm communication.

## Testing & safety

- This is **pre-release** software that has not touched real hardware yet.
  Validate every step on the bench before trusting it on a running engine.
- OBD1 real-time tuning with an Ostrich is lower-risk than flashing (edits go
  to the emulator's RAM, not a chip), but still: keep the laptop on AC power and
  don't rely on this for a final chip burn until it's proven.

## Roadmap

- [x] Decode the serial protocol from the original binary
- [x] Cross-platform serial hardware layer (Ostrich + HULOG)
- [x] Managed device discovery (replaces `snake4hts32.dll`)
- [x] Avalonia UI shell that scans + handshakes
- [x] Named datalog channels + CSV logging (original column order)
- [x] ROM image + definition-driven table engine (load / read / write cells)
- [x] Dark, tabbed HTS-style UI (Connect / Tune / Datalog)
- [x] Heatmap map-table editor (edit cells, live recolor, save ROM)
- [x] Live gauge dashboard + demo mode (animates without hardware)
- [ ] Full ECU datalog frame decode + validated channel scaling
- [ ] Real-time table push to the Ostrich while logging
- [ ] Verified P28 table definitions (addresses/scaling)
- [ ] Wideband (Innovate MTS) input
- [ ] Hardware-in-the-loop testing on a real P28 + Ostrich + HULOG

## Tuning definitions

Table locations live in external JSON definition files (see `defs/`, similar to
a TunerPro XDF) rather than being hardcoded. `defs/p28.template.json` is a
**placeholder with unverified addresses** — replace it with values validated
against a known-good P28 definition before editing a live tune. Wrong
fuel/ignition addresses or scaling produce dangerous maps.

## Licensing note

The original HTS is third-party proprietary software. This project is a
clean-room reimplementation built for **personal interoperability** (running
tools you own on your own hardware). The decompiled original source is **not**
included in this repository and should not be redistributed. If you plan to
share this port, get the original author's blessing first.
