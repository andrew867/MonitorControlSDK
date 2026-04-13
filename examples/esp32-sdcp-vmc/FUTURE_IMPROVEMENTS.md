# Future improvements — `esp32-sdcp-vmc`

Backlog and design notes for the native SDCP + WiFi + SDAP + web provisioning sketch (`monitor_knobs_sdcp.ino`, `wifi_sdap_web.ino`, `config_portal.h`). Nothing here is committed work; use it for roadmap, forks, or product spin-offs.

---

## Security and deployment

- **Change the default soft-AP password** (`monitorctl` in `wifi_sdap_web.ino` / `WiFi.softAP`) before any non-lab deployment; consider a **per-device random password** printed on a sticker or shown once on serial after first flash.
- **HTTP basic auth** (or a simple token in a header) on the STA config server (**port 8080**) so anyone on the LAN cannot change WiFi or monitor IP without credentials.
- Document **control VLAN** / firewall rules: treat **8080** like management — no exposure to guest WiFi or untrusted segments.
- Optional **HTTPS** on STA is heavy on ESP32; if TLS is required, prefer a **reverse proxy** on a small SBC or use the [HTTP + gateway](../arduino-knobs-brightness-contrast/) path instead.

---

## Connectivity and operator UX

- **mDNS** (e.g. `http://monitorctrl.local`) in STA mode so operators do not need to read the serial banner or DHCP lease for the device IP.
- **Captive portal polish**: clearer captive detection on iOS/Android; optional **“open config”** deep link from serial.
- **WiFi scan UX**: RSSI bars, hidden-SSID manual entry, **5 GHz vs 2.4 GHz** hint (ESP32 is 2.4 GHz only — say so in UI).
- **WPA2-Enterprise** (802.1X) if venues require it — significant firmware and UI work.
- **Improv Wi-Fi** (or similar) for **BLE-assisted** provisioning where soft-AP is disallowed.

---

## Firmware lifecycle and reliability

- **Arduino `HTTPUpdate`** or **Espressif `esp_https_ota`** with a documented release URL; show **firmware version** and build date in the web UI and serial banner.
- **STA reconnect policy**: exponential backoff, optional “fall back to config AP if STA fails N times” without erasing NVS.
- **Watchdog** tuned for SDCP + web paths so a hung `WiFiClient::read` cannot brick the device indefinitely.
- **NVS migration** if keys or layout change (`mcfg` version byte or separate namespace).

---

## SoC and board coverage

- **ESP32-C3** (and other RISC-V WiFi parts): this tree currently targets **classic ESP32 / S2 / S3** with compile-time ADC defaults. A C3 port needs a **verified pin table**, `#error` / feature gates lifted, and real hardware QA — or steer users to the **HTTP gateway** example on C3-class boards.
- **Board-specific README snippets** or a small table file for popular modules (DevKitC, S3-DevKitC-1, Feather ESP32, etc.) with strapping-pin warnings.
- **Optional `sdkconfig` / PlatformIO** variant for teams that do not use Arduino IDE merge order.

---

## Protocol and features (firmware)

- **UDP multi-monitor / Group-All** SDCP on **53484** (parity with .NET `VmcUdpBroadcastClient`) — higher complexity on WiFi; document tradeoffs before implementing.
- **Longer or configurable SDAP listen** windows; optional **continuous background SDAP** with rate limiting (careful with CPU and WiFi coexistence).
- **Serial/Web “test SDCP”** ping: open TCP, one `STATget MODEL`, show OK/fail without touching pots.

---

## Hardware product ideas (niche broadcast)

- **Small OLED (I²C)** for SSID, STA IP, monitor host, last SDAP hit, and last VMC error — reduces phone/laptop dependency beside a rack or truck.
- **PoE** (802.3af/at) on a derivative board: single cable for install; mind **isolation** and **EMC** if you attach shielded Ethernet near analog knobs.
- **One high-quality encoder** for fine grading in addition to pots, if BOM allows.
- **Enclosure + panel mount + silkscreened GPIO map** matter more than extra features for resale credibility.

---

## Documentation and repo hygiene

- Keep [docs/diagrams/monitor-control-flows.md](../../docs/diagrams/monitor-control-flows.md) in sync when discovery or transport paths change.
- Optional **wiring photo** or Fritzing for the default DevKit + pots layout.
- **CHANGELOG** entries when user-visible behaviour or security defaults change.

---

## Build and CI (optional)

- **arduino-cli** compile matrix in CI for `esp32:esp32` board package (e.g. `esp32`, `esp32s3`) to catch merge-order and API breakages — no hardware required for a smoke compile.
