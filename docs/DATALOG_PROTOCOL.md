# HTS datalog protocol (reverse-engineered)

Recovered by decompiling the original `HTS 2.22 F2 Snake_Edition.exe` and reading
its datalog code. This documents the on-wire format so the native app can decode
real ECU data. Items marked **(verify)** are best-effort from static analysis and
should be confirmed against a live capture from the original HTS.

## Transport

- Serial over the HULOG's FTDI virtual COM port.
- Command packets are framed with an ASCII **`DR`** header (`0x44 0x52`) followed
  by command/parameter bytes and a checksum. The firmware answers commands with
  `0x4F` (`'O'`, OK). **(verify: exact online-stream request sequence)**
- While logging, the ECU streams **fixed 64-byte data frames**.

## Data frame (64 bytes)

- **Checksum**: the app computes the running sum of the frame bytes
  (`sum += frame[i]`) and compares it against the frame's checksum byte; mismatched
  frames are discarded. **(verify: checksum byte position — appears to be the last
  byte)**
- **16-bit fields** are little-endian: `value = lo + hi * 256`
  (from `(ushort)(hi * 256 + lo)`), except the aux/analog inputs which assemble as
  `hi * 256 + lo` with the high byte first.

### Byte offset map

| Offset | Field | Channel | Conversion | Notes |
|---|---|---|---|---|
| 0 | header | frame marker | — | frame counter / type |
| 1 | ect | Coolant temp | `raw / 4` °C | confirmed |
| 2 | iat | Intake air temp | `raw / 4` °C | confirmed |
| 3 | ecuO2V | ECU O2 voltage | voltage scale | **(verify)** |
| 4 | baro | Barometric | `(raw/2 + 24) * 7.221 − 59` kPa | confirmed |
| 5 | map | MAP | `(raw/2 + K) * 7.221 − 59` kPa | Honda formula; K≈24 |
| 6 | tps | Throttle % | `raw * 100 / 255` (ROM-cal) | **(verify)** |
| 7–8 | rpm | Engine RPM | 16-bit LE, scale **(verify)** | see note |
| 9 | flags | status bits | bitfield | fuel-cut, VTEC, etc. |
| 10–16 | — | misc | — | |
| 17 | vss | Vehicle speed | `raw * 0.1` | confirmed |
| 18–19 | inj | Injector duration | 16-bit LE, `/ 4` ms | confirmed scale |
| 20 | ignFnl | Ignition (final) | degrees | **(verify)** |
| 21 | ignTbl | Ignition (table) | degrees | **(verify)** |
| 22 | inputs | input bits | bitfield | PSP, SCC, A/C… |
| 23–24 | outputs | output bits | bitfield | IAB, VTEC, fan… |
| 25 | eldV | ELD | voltage scale | **(verify)** |
| 26 | batV | Battery | `26 * raw / 270` V | confirmed |
| 27 | ectFc | ECT fuel corr | — | |
| 28–29 | o2Short | O2 short-term | 16-bit LE | |
| 30–31 | o2Long | O2 long-term | 16-bit LE | |
| 32–33 | iatFc | IAT fuel corr | 16-bit LE | |
| 34–49 | — | corrections / IO | — | veFc, gear, etc. |
| 50–51 | — | 16-bit | — | |
| 53–62 | analog1..5 | Aux analog inputs | `(hi*256+lo) * 255/1024` | 5×16-bit, high byte first |

**RPM note:** the decompiled getter divides by 32768 in one code path, which does
not by itself yield engine RPM — the exact scaling is overloaded and ambiguous in
static analysis. The frame *position* (bytes 7–8, little-endian) is solid; the
multiplier is the main thing to confirm from a live capture.

## How this was validated

- Frame size, checksum method, offsets, and the 16-bit assembly are read directly
  from the frame-fill routine and the master sensor `switch`.
- Conversions marked "confirmed" are simple constants read straight from the code
  (`/4`, `*0.1`, `26*raw/270`, the `*7.221 − 59` MAP/baro formula).
- ROM-calibrated channels (MAP mode, TPS %, AFR) depend on values stored in the
  loaded ROM, so their exact output needs either the ROM's calibration or a live
  capture to pin down.

## Next step to make it exact

Run the original HTS in a Windows VM with the HULOG connected, capture a short
datalog, and note a few known values (idle RPM, coolant temp, battery voltage).
Comparing those against the raw frame bytes confirms every scale factor above.
