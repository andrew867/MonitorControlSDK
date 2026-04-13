# Web API, browser UI, and Python gateway

This repository ships a **production-style HTTP API** on top of the same SDAP/SDCP stack used by the CLI and samples. Any frontend (React, Vue, mobile) or backend language can integrate via JSON over HTTP.

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

See [`examples/python-service/README.md`](../../examples/python-service/README.md). It proxies `/api/*` to the .NET base URL and serves the same static UI from port **8000** by default.

## Integration checklist for third-party web apps

1. Run `MonitorControl.Web` (or proxy through the Python gateway).
2. Call `GET /api/sdap/discover` to learn monitor IPs (or use fixed inventory).
3. Use `POST /api/vmc/get` / `vmc/set` for shading and UI automation.
4. Use `POST /api/vms/product-info` for structured factory reads.
5. Restrict firmware routes to authenticated operators; keep firmware disabled in production unless you fully control the OTA sequence.

## Source files

- Route registration: [`MonitorApiExtensions.cs`](../../src/MonitorControl.Web/MonitorApiExtensions.cs)
- Firmware gate: [`WireFormat.cs`](../../src/MonitorControl.Web/WireFormat.cs)
- UI: [`wwwroot/`](../../src/MonitorControl.Web/wwwroot/)
