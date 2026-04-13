# Examples (non-.NET and gateways)

These folders are **optional** integration patterns around the same SDAP/SDCP behavior as the core library. They are versioned with the solution so teams can adopt the stack from Python or Arduino (ESP32 / ESP8266) without embedding .NET on the device.

**Index of diagrams** (MCU paths, Python proxy, REPL): [docs/diagrams/monitor-control-flows.md](../docs/diagrams/monitor-control-flows.md).

**HTTP API reference** (every route, firmware gate, push semantics): [docs/guide/web-api-and-python-gateway.md](../docs/guide/web-api-and-python-gateway.md).

**OpenAPI 3** (offline + codegen): [openapi/monitorcontrol.openapi.json](../openapi/monitorcontrol.openapi.json), [openapi/README.md](../openapi/README.md).

---

## [python-service/](python-service/)

| Aspect | Detail |
|--------|--------|
| **Stack** | FastAPI + `httpx` + `uvicorn` |
| **Role** | Reverse proxy: browser hits Python **:8000**; `/api/*` forwards to `MONITOR_CONTROL_API_URL` (default `http://127.0.0.1:5080`). Static UI copied alongside the .NET `wwwroot` experience. |
| **SSE** | `GET /api/events/...` is streamed end-to-end (`StreamingResponse` in [`main.py`](python-service/main.py)). |
| **WebSocket** | **Not implemented in the proxy.** Clients must use `ws://<dotnet-host>:<dotnet-port>/ws/monitor-watch?...` (same host as `MonitorControl.Web`). |
| **Firmware POSTs** | Header `X-Firmware-Ack` is forwarded when present; the .NET process must still allow dangerous firmware via config/env. |

Full runbook: [python-service/README.md](python-service/README.md).

---

## [arduino-knobs-brightness-contrast/](arduino-knobs-brightness-contrast/)

| Aspect | Detail |
|--------|--------|
| **Hardware** | ESP32 (two ADC channels) or ESP8266 (single `A0` — see README for limitations). |
| **Sketch** | `monitor_knobs_http.ino` |
| **Control path** | WiFi → **`POST /api/vmc/set`** with JSON `host` + `args` → **MonitorControl.Web** → `VmcClient` → TCP **53484**. |
| **Why HTTP** | Avoids hand-serializing SDCP v3 on the MCU; gateway owns framing ([`SdcpMessageBuffer`](../src/MonitorControlSDK/Protocol/SdcpMessageBuffer.cs)). |
| **Calibration** | Serial commands `cap bmin` / `bmax` / `cmin` / `cmax`; NVS (`kbcal`) on ESP32, EEPROM blob on ESP8266. |

Full wiring and defines: [arduino-knobs-brightness-contrast/README.md](arduino-knobs-brightness-contrast/README.md).

**Alternative on ESP32:** same pots/buttons concept but **no HTTP** — use [esp32-sdcp-vmc/](esp32-sdcp-vmc/) for native TCP SDCP.

---

## [esp32-sdcp-vmc/](esp32-sdcp-vmc/)

| Aspect | Detail |
|--------|--------|
| **Hardware** | **ESP32 only** (`#error` on non-ESP32) — WiFi + `WiFiClient`. |
| **Sketch** | `monitor_knobs_sdcp.ino` |
| **Control path** | WiFi → **TCP connect monitor IP port 53484** → write **SDCP v3** frames with item **0xB000** and ASCII `STATget` / `STATset` in the data area → read response, check response byte **OK**. |
| **Parity** | Header construction matches C# layout documented in [sdcp-framing-and-items.md](../docs/reference/sdcp-framing-and-items.md); see handbook §5.4. |
| **Modes** | **PICTURE** (brightness/contrast), **RGB_GAIN**, **GRADE** (aperture/chroma/phase); **MODE** / **CAL** / **POWER** buttons; serial `help`, `cap …`, `flat on/off`, `cal show/reset`. |
| **Discovery** | Sketch uses **`MONITOR_HOST`**; it does **not** listen on SDAP. Use PC `monitorctl discover` or `Sample.Discovery` once, then hard-code or provision IP. |
| **UDP multi-monitor** | Not in this sketch; use .NET [`VmcUdpBroadcastClient`](../src/MonitorControlSDK/Clients/VmcUdpBroadcastClient.cs) or `POST /api/vmc/broadcast` for Group/All. |

Full GPIO table, VMC tokens, and safety notes: [esp32-sdcp-vmc/README.md](esp32-sdcp-vmc/README.md).
