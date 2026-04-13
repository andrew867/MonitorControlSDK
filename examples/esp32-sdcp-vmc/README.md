# Native SDCP / VMC on ESP32 (no PC gateway)

Arduino sketch(es) open **TCP port 53484** to the monitor, send **SDCP v3** frames with **item `0xB000`**, and embed ASCII **`STATset …`** / **`STATget …`** in the data area — the same framing as [`SdcpMessageBuffer`](../../src/MonitorControlSDK/Protocol/SdcpMessageBuffer.cs) + [`LegacyVmcContainer`](../../src/MonitorControlSDK/Internal/LegacyVmcContainer.cs). (Multi-monitor **UDP** SDCP on the same port lives in the .NET SDK as [`VmcUdpBroadcastClient`](../../src/MonitorControlSDK/Clients/VmcUdpBroadcastClient.cs); this firmware stays **TCP-first** for predictable WiFi stacks.)

## Files in this folder

| File | Role |
|------|------|
| `monitor_knobs_sdcp.ino` | Pots, buttons, SDCP transactions, serial CLI |
| `wifi_sdap_web.ino` | NVS `mcfg` (WiFi + monitor IP), **SDAP** UDP **53862**, **WebServer** + **DNSServer** captive portal |
| `config_portal.h` | Single-page **HTML5** UI (dark theme, mobile-first) in PROGMEM |

Arduino IDE **merges every `.ino` in the folder** into one build (alphabetical: `monitor_knobs_sdcp.ino` then `wifi_sdap_web.ino`).

## First boot — WiFi and web config

1. Flash the sketch. If **no WiFi SSID** is stored in NVS namespace **`mcfg`**, the device starts a soft-AP:
   - SSID pattern: **`MonitorCtrl-XXXX`** (last octets of MAC)
   - Password: **`monitorctl`**
   - Join that WiFi from a phone or laptop, then open **`http://192.168.4.1/`** (captive DNS redirects common probe hosts to the ESP).
2. Enter your **home/office Wi‑Fi SSID and password**, **Save** → device **reboots** and connects as **STA**.
3. On STA, the same UI is served on **`http://<device-ip>:8080/`** (see serial banner or send `web` over UART).
4. **SDAP discover** in the browser listens on **UDP 53862** for several seconds and lists monitors (product, serial, **Connection IP**). Pick the row and save **Monitor IP**, or type it manually. **SDAP only works when the ESP32 is on the same Ethernet/VLAN as the monitors** (not while you are only associated with the ESP’s setup AP).

NVS keys (`mcfg`): `ssid`, `pass`, `mhost` (monitor IPv4 string).

## Serial (115200 baud)

| Command | Action |
|---------|--------|
| `help` | Lists commands |
| `discover [ms]` | Listen **UDP 53862** for SDAP (default **5000** ms), print rows |
| `portal` | Stop STA shading and open the **config AP** again |
| `web` | Print `http://<ip>:8080/` when STA is up |
| `mode pic` / `rgb` / `grade` | Same as MODE button |
| `cap …` / `flat` / `cal …` | Calibration (see in-sketch help) |

## Modes (MODE button or serial)

| Mode | Pots (A / B / C) | VMC |
|------|------------------|-----|
| **PICTURE** | Brightness / Contrast / (C unused) | `BRIGHTNESS`, `CONTRAST` |
| **RGB_GAIN** | R / G / B drive | `RGAIN`, `GGAIN`, `BGAIN` |
| **GRADE** | Aperture / Chroma / Phase | `APERTURE` (0–6), `CHROMA`, `PHASE` (0–100) |

## Board support and ADC pins

Defaults are chosen for common silicon; **override before `#include` order** by defining at the **top** of `monitor_knobs_sdcp.ino` (before the auto pin block):

```cpp
#define PIN_ADC_A 15
#define PIN_ADC_B 16
#define PIN_ADC_C 17
```

| SoC / dev board | Default `PIN_ADC_A/B/C` | Notes |
|-----------------|-------------------------|--------|
| **ESP32** (WROOM/WROVER, DevKitC, most “30‑pin” boards) | **34 / 35 / 32** | ADC1 pins; good coexistence with WiFi |
| **ESP32-S3** (DevKitC-1, many N8R2/N16R8 modules) | **4 / 5 / 6** | Only when `CONFIG_IDF_TARGET_ESP32S3` is set by the core |
| **ESP32-S2** | **1 / 2 / 3** | Fewer ADC channels; verify your module pinout |

**ESP32-C3** is **not** supported by this sketch (`#error` targets full ESP32 with required ADC layout); use the [HTTP + gateway](../arduino-knobs-brightness-contrast/) path on RISC-V MCUs instead.

Buttons default to **25 / 26 / 27** (change with `#define PIN_BTN_MODE` etc. if your board uses those strapping pins).

## Security notes

- The setup AP uses a **shared default password** (`monitorctl`) — fine for lab provisioning; **change the softAP password in `wifi_sdap_web.ino`** (`WiFi.softAP`) before any sensitive environment.
- The web UI has **no login** on port **8080** on your LAN — isolate the device on a **control VLAN** or firewall **8080** from untrusted clients.

## Calibration, power, wiring tables

Same behaviour as before: **CAL** / **POWER**, NVS **`kbcal`**, and GPIO table — see previous sections in git history or the sketch comments. Wiring table in-code matches the **SoC defaults** above.

## When to use HTTP instead

- **ESP8266**, **TLS**, central auth, or you do not want to maintain SDCP framing on the MCU → [`../arduino-knobs-brightness-contrast/`](../arduino-knobs-brightness-contrast/).

## Diagrams and parity with C#

[docs/diagrams/monitor-control-flows.md](../../docs/diagrams/monitor-control-flows.md) · [engineering handbook](../../docs/handbook.md) §5.4

## Reference

- [SDCP framing and item numbers](../../docs/reference/sdcp-framing-and-items.md)
- [spec/sdap-overview.md](../../docs/spec/sdap-overview.md) + [`SdapAdvertisementPacket`](../../src/MonitorControlSDK/Protocol/SdapAdvertisementPacket.cs)
- [VMC command surface](../../docs/reference/vmc-command-surface.md)
- [spec/vmc-string-catalog.md](../../docs/spec/vmc-string-catalog.md)
- [PVM-740 VMC catalog (manual-derived)](../../docs/reference/appendices/pvm-740-vmc-catalog-from-manual.txt)
