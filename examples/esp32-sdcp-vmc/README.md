# Native SDCP / VMC on ESP32 (no HTTP gateway)

`monitor_knobs_sdcp.ino` opens **TCP port 53484** to the monitor, sends **SDCP v3** frames with **item `0xB000`**, and embeds ASCII **`STATset …`** lines in the data area — the same framing as [`SdcpMessageBuffer.setupVmcPacketHeader`](../../src/MonitorControlSDK/Protocol/SdcpMessageBuffer.cs) + [`LegacyVmcContainer.setCommand`](../../src/MonitorControlSDK/Internal/LegacyVmcContainer.cs). (Multi-monitor **UDP** SDCP on the same port is implemented in the .NET SDK as [`VmcUdpBroadcastClient`](../../src/MonitorControlSDK/Clients/VmcUdpBroadcastClient.cs); this sketch stays TCP-first for reliable WiFi stacks.)

## Modes (MODE button or serial)

| Mode | Pots (A / B / C) | VMC |
|------|------------------|-----|
| **PICTURE** | Brightness / Contrast / (C unused) | `BRIGHTNESS`, `CONTRAST` |
| **RGB_GAIN** | R / G / B drive | `RGAIN`, `GGAIN`, `BGAIN` |
| **GRADE** | Aperture / Chroma / Phase | `APERTURE` (0–6), `CHROMA`, `PHASE` (0–100) — ranges aligned with PVM-740 manual examples |

**GRADE** is a practical “third lane” for grading / signal-path tweaks where HDR-style controls are often not exposed as simple `STATset` numerics on older VMC paths; validate tokens on your chassis.

## Calibration helpers

- **CAL short press**: toggles `FLATFIELDPATTERN ON` / `OFF` (flat field for setup).
- **CAL long press (~1.5 s)**: sends `WBSEL USER` then `FLATFIELDPATTERN ON` (white-balance prep pattern on many OEM manuals — confirm on device).
- **Serial**: `flat on` / `flat off`, `cal show`, `cal reset`, and extended `cap …` endpoints (see `help` on serial).

NVS namespace **`kbcal`** stores per-mode ADC endpoints (`b0/b1`, `c0/c1`, `r0/r1`, `g0/g1`, `bl0/bl1`, `a0/a1`, `ch0/ch1`, `ph0/ph1`).

## Power / standby (POWER button)

Each press alternates **`STATset POWERSAVING OFF`** and **`STATset POWERSAVING ON`** (PVM-740 catalog). Semantics differ by model; some chassis use different strings or reject VMC when already in standby. Treat as an **example** and adjust `onPowerPress()` if your monitor documents a different control.

## Wiring (defaults in sketch)

| Function | GPIO | Notes |
|----------|------|--------|
| ADC A | 34 | Bright / R gain / Aperture |
| ADC B | 35 | Contrast / G gain / Chroma |
| ADC C | 32 | B gain / Phase (ADC1; OK with WiFi on typical ESP32) |
| MODE | 25 | Momentary to GND, `INPUT_PULLUP` |
| CAL | 26 | Momentary to GND |
| POWER | 27 | Momentary to GND |

## When to use this

- The ESP32 is on the **same LAN** as the monitor and you want **no PC** in the control path.
- You accept maintaining **wire compatibility** if the SDK header layout ever changes (unlikely for V3 VMC).

## When to use HTTP instead

- **ESP8266** (limited ADC), **TLS**, or **central auth/rate limits** → [`../arduino-knobs-brightness-contrast/`](../arduino-knobs-brightness-contrast/) calling `MonitorControl.Web`.

## Diagrams and parity with C#

End-to-end **integration matrix**, **ESP32 native sequence** (this sketch), **Python SSE vs WebSocket ports**, and **dual knob paths** (HTTP vs on-wire): [docs/diagrams/monitor-control-flows.md](../../docs/diagrams/monitor-control-flows.md). The [engineering handbook](../../docs/handbook.md) §5.4 lists byte-level parity between `buildVmcPacket` and [`SdcpMessageBuffer`](../../src/MonitorControlSDK/Protocol/SdcpMessageBuffer.cs).

## Reference

- [SDCP framing and item numbers](../../docs/reference/sdcp-framing-and-items.md)
- [VMC command surface](../../docs/reference/vmc-command-surface.md)
- [spec/vmc-string-catalog.md](../../docs/spec/vmc-string-catalog.md)
- [PVM-740 VMC catalog (manual-derived)](../../docs/reference/appendices/pvm-740-vmc-catalog-from-manual.txt)
- [Mermaid flows](../../docs/diagrams/monitor-control-flows.md)
