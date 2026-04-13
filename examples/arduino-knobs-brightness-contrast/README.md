# ADC knobs → brightness / contrast (ESP32 / ESP8266)

`monitor_knobs_http.ino` reads **two analog inputs** on **ESP32** (pots or dividers), maps them to integers, and sends **`STATset BRIGHTNESS`** / **`STATset CONTRAST`** by calling **MonitorControl.Web** over WiFi (`POST /api/vmc/set`). On **ESP8266**, only **`A0`** is a true ADC — the sketch still compiles and posts both tokens, but both values come from the same pin unless you add an external ADC (for example ADS1115) and change `readMapped` for the contrast channel.

## Why HTTP instead of raw SDCP on the MCU?

SDCP uses a **binary V3 header + item 0xB000** payload layout ([framing doc](../../docs/reference/sdcp-framing-and-items.md)). Implementing that correctly on Arduino is possible but error-prone. A small LAN PC, NUC, or Raspberry Pi running `dotnet run --project src/MonitorControl.Web` is the **simplest bridge**: the MCU only needs WiFi + JSON.

## Wiring

- **3.3 V** MCUs: connect pot ends to **3V3** and **GND**, wiper to `ADC_BRIGHT_PIN` / `ADC_CONTRAST_PIN` (defaults in sketch).
- Add a **100 nF–1 µF** cap from each wiper to GND to quiet ADC noise if cables are long.
- **ESP8266** has a **0–1 V** ADC range on the TOUT pin on some modules; many dev boards use a voltage divider — check your board and adjust `ADC_MAX`.

## Calibration (NVS / EEPROM)

After flashing, open the serial monitor at **115200 baud**:

- Put the **brightness** pot at its physical minimum, then send `cap bmin`. Repeat at maximum with `cap bmax`.
- Same for contrast: `cap cmin`, `cap cmax`.
- `cal show` prints stored ADC endpoints; `cal reset` restores full-scale mapping.

ESP32 uses **Preferences** (`kbcal` namespace). ESP8266 uses a small **EEPROM** blob.

## Configure the sketch

Edit the `/* ---- User configuration ---- */` block:

| Define | Meaning |
|--------|---------|
| `WIFI_SSID` / `WIFI_PASSWORD` | LAN credentials |
| `GATEWAY_HOST` | IP of the machine running MonitorControl.Web (not necessarily the monitor) |
| `GATEWAY_PORT` | Usually `5080` |
| `MONITOR_HOST` | Monitor’s SDCP IP (same value you use in the web UI “host”) |
| `ADC_*` pins and `VMC_*_MIN` / `MAX` | Range your chassis accepts (try `get BRIGHTNESS` from CLI/UI first) |

## Run the gateway

```bash
dotnet run --project src/MonitorControl.Web --urls http://0.0.0.0:5080
```

Bind to `0.0.0.0` so devices on the LAN can reach the API (firewall permitting).

## Flow diagrams

See [docs/diagrams/monitor-control-flows.md](../../docs/diagrams/monitor-control-flows.md).
