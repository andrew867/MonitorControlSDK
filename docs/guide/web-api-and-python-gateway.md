# Web API, browser UI, and Python gateway

This repository ships a **production-style HTTP API** on top of the same SDAP/SDCP stack used by the CLI and samples. Any frontend (React, Vue, mobile) or backend language can integrate via JSON over HTTP.

**Diagrams:** [Control flow charts (Mermaid)](../diagrams/monitor-control-flows.md) — clients, gateway, SDAP/SDCP, and ADC knob path. **MCU samples:** [arduino-knobs (HTTP)](../../examples/arduino-knobs-brightness-contrast/), [esp32-sdcp-vmc (native SDCP)](../../examples/esp32-sdcp-vmc/). **OpenAPI → C:** [openapi-codegen.md](openapi-codegen.md).

## Components

| Piece | Path | Role |
|-------|------|------|
| **HTTP + JSON + Swagger + static UI** | [`src/MonitorControl.Web`](../../src/MonitorControl.Web) | ASP.NET Core 8 host (`MonitorControl.Web`) |
| **Python reverse proxy + static UI** | [`examples/python-service`](../../examples/python-service) | Optional `uvicorn` + `httpx` gateway to the .NET API |

## Run the .NET web host

```bash
dotnet run --project src/MonitorControl.Web --urls http://127.0.0.1:5080
```

- **Browser UI:** `http://127.0.0.1:5080/` — discover, VMC, VMS, VMA reads, firmware (guarded).
- **OpenAPI:** `http://127.0.0.1:5080/swagger`
- **Health:** `GET /api/health`

## API summary

All JSON bodies use **camelCase** by default (`host`, `timeoutMs`, …).

| Method | Path | Body | Notes |
|--------|------|------|--------|
| GET | `/api/health` | — | Liveness |
| GET | `/api/sdap/discover` | query `durationMs`, optional `bind` | UDP 53862 listen window; returns unique devices |
| POST | `/api/vmc/get` | `{ "host", "field", "timeoutMs"? }` | `STATget` |
| POST | `/api/vmc/set` | `{ "host", "args": ["TOKEN","…"], "timeoutMs"? }` | `STATset` tail |
| POST | `/api/vmc/broadcast` | `{ "scope": "all"|"group", "groupId"?, "broadcastAddress"?, "port"?, "localBind"?, "tokens": ["STATset","BRIGHTNESS","512"] }` | **UDP** SDCP VMC to **53484** (no TCP `host`; affects every monitor in scope — use with care) |
| GET | `/api/events/monitor` | query `host`, optional `fields` (comma `STATget` names), `intervalMs` | **SSE** (`text/event-stream`): server polls `STATget` and pushes JSON lines. Custom event `fault` carries SDCP/TCP errors. |
| GET | `/ws/monitor-watch` | query `host`, optional `fields`, `intervalMs` | **WebSocket** (binary/text UTF‑8 JSON objects): same poll model as SSE. |
| POST | `/api/vms/product-info` | `{ "host", "timeoutMs"? }` | VMS product info + hex payload |
| POST | `/api/vma/control-software-version` | `{ "host", "timeoutMs"? }` | VMA read |
| POST | `/api/vma/kernel-version` | same | |
| POST | `/api/vma/rtc` | same | |
| POST | `/api/vma/fpga1-version` | same | |
| POST | `/api/vma/fpga2-version` | same | |
| POST | `/api/vma/fpga-core-version` | same | |
| POST | `/api/vma/firmware/upgrade-kernel-size` | `{ "host", "sizeBytes", "timeoutMs"? }` | **Danger** — see below |
| POST | `/api/vma/firmware/upgrade-fpga-size` | `{ "host", "sizeBytes", "timeoutMs"? }` | **Danger** |
| POST | `/api/vma/firmware/upgrade-chunk` | `{ "host", "chunkIndex", "timeoutMs"? }` | **Danger** |
| POST | `/api/vma/firmware/upgrade-restart` | `{ "host", "timeoutMs"? }` | **Danger** |

### Firmware gate (mandatory)

Firmware routes return **403** unless **both**:

1. Server: `MonitorControl:AllowDangerousFirmware` = `true` in `appsettings.json` **or** environment variable `MONITOR_CONTROL_ALLOW_DANGEROUS_FIRMWARE=1` / `true` / `yes`.
2. Request header: `X-Firmware-Ack: CONFIRM` (exact value, case-sensitive).

The bundled HTML UI sets this header when the operator checks the confirmation box.

### SDAP bind conflicts

Only one process can bind UDP **53862** on a given interface. If discover fails with “bind failed”, stop other tools using SDAP on that machine or pass `bind` to a free adapter address.

## Python gateway

See [`examples/python-service/README.md`](../../examples/python-service/README.md). It proxies **`/api/*`** to the .NET base URL and serves the same static UI from port **8000** by default.

### SSE vs WebSocket when using Python

- **SSE** (`GET /api/events/monitor?...`): proxied — the browser can stay on **:8000** and the FastAPI app streams from the upstream .NET URL ([`main.py`](../../examples/python-service/main.py) detects `full_path.startswith("events/")` and uses `httpx` streaming).
- **WebSocket** (`GET /ws/monitor-watch`): **not proxied** by `python-service`. Open the WebSocket to the **MonitorControl.Web** listener (e.g. `ws://127.0.0.1:5080/ws/monitor-watch?host=...`), same host/port as Swagger.

See [diagrams/monitor-control-flows.md](../diagrams/monitor-control-flows.md) (Python gateway figure).

## Push-style updates (SSE / WebSocket)

SDCP in this stack is **request/response** on TCP; the monitor does not open an outbound HTTP channel. The **SSE** and **WebSocket** routes synthesize “live” updates by **polling `STATget`** on the server at `intervalMs` (default 2000 ms). Tune `fields` to tokens your chassis supports (defaults: `MODEL`, `BRIGHTNESS`, `CONTRAST`).

- **Browser:** bundled UI → “Live snapshots (SSE)” uses `EventSource`.
- **WebSocket URL:** `ws://<host>:<port>/ws/monitor-watch?host=<monitor-ip>&intervalMs=3000` (use `wss://` behind TLS).

## Integration checklist for third-party web apps

1. Run `MonitorControl.Web` (or proxy **REST + SSE** through the Python gateway on **:8000** while keeping .NET on **:5080**).
2. Call `GET /api/sdap/discover` to learn monitor IPs (or use fixed inventory). Only one process should bind UDP **53862** per interface.
3. Use `POST /api/vmc/get` / `vmc/set` for shading and UI automation; optional `POST /api/vmc/broadcast` for UDP Group/All (same token list as `vmc/set` but **no** per-monitor TCP response).
4. Use `GET /api/events/monitor` (SSE) and/or **WebSocket** `GET /ws/monitor-watch` for push-shaped updates. If you use **python-service**, SSE can go through **:8000**; WebSocket must target the **.NET** port (see [diagrams/monitor-control-flows.md](../diagrams/monitor-control-flows.md)).
5. Use `POST /api/vms/product-info` for structured factory reads.
6. Restrict firmware routes to authenticated operators; keep firmware disabled in production unless you fully control the OTA sequence.

## Source files

- Route registration: [`MonitorApiExtensions.cs`](../../src/MonitorControl.Web/MonitorApiExtensions.cs), [`MonitorPushEndpoints.cs`](../../src/MonitorControl.Web/MonitorPushEndpoints.cs)
- Firmware gate: [`WireFormat.cs`](../../src/MonitorControl.Web/WireFormat.cs)
- UI: [`wwwroot/`](../../src/MonitorControl.Web/wwwroot/)
