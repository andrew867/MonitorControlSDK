# Native SDCP / VMC on ESP32 (no HTTP gateway)

`monitor_knobs_sdcp.ino` opens **TCP port 53484** to the monitor, sends **SDCP v3** frames with **item `0xB000`**, and embeds ASCII **`STATset BRIGHTNESS n`** / **`STATset CONTRAST n`** in the data area — the same framing as [`SdcpMessageBuffer.setupVmcPacketHeader`](../../src/MonitorControlSDK/Protocol/SdcpMessageBuffer.cs) + [`LegacyVmcContainer.setCommand`](../../src/MonitorControlSDK/Internal/LegacyVmcContainer.cs).

## When to use this

- The ESP32 is on the **same LAN** as the monitor and you want **no PC** in the control path.
- You accept maintaining **wire compatibility** if the SDK header layout ever changes (unlikely for V3 VMC).

## When to use HTTP instead

- **ESP8266** (limited ADC), **TLS**, or **central auth/rate limits** → [`../arduino-knobs-brightness-contrast/`](../arduino-knobs-brightness-contrast/) calling `MonitorControl.Web`.

## Calibration

Same serial commands and NVS namespace `kbcal` as the HTTP sketch (`cap bmin` … `cal show`).

## Reference

- [SDCP framing and item numbers](../../docs/reference/sdcp-framing-and-items.md)
- [Mermaid flows](../../docs/diagrams/monitor-control-flows.md)
