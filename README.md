# MonitorControlSDK — Sony monitor SDAP / SDCP

.NET 8 client library (**Sony.MonitorControl**), operator CLI (**monitorctl**), and samples for SDAP discovery and SDCP control of compatible Sony professional monitors.

**Upstream (HTTPS):** [https://github.com/andrew867/MonitorControlSDK](https://github.com/andrew867/MonitorControlSDK)

**License:** [LICENSE](LICENSE) (MIT, Copyright 2026 Andrew Green).

## Documentation (start here)

Full tutorial + opcode reference + firmware guide: **[docs/index.md](docs/index.md)**  
**10-minute path:** [docs/quickstart.md](docs/quickstart.md)  
**Legacy `references/` parity (every subtree mapped):** [docs/reference/references-parity.md](docs/reference/references-parity.md)

## Quick commands

```bash
dotnet build Sony.MonitorControl.sln -c Release
dotnet test Sony.MonitorControl.sln -c Release
dotnet run --project src/MonitorControl.Cli -- discover
dotnet run --project src/MonitorControl.Cli -- vmc --host 192.168.0.10 MODEL
dotnet run --project src/MonitorControl.Cli -- vmc-broadcast --scope all -- STATset BRIGHTNESS 512
```

## HTTP API + web UI (for any frontend)

```bash
dotnet run --project src/MonitorControl.Web --urls http://127.0.0.1:5080
```

Then open `http://127.0.0.1:5080/` (UI) and `http://127.0.0.1:5080/swagger` (OpenAPI). Optional Python proxy: [examples/python-service/README.md](examples/python-service/README.md). Full doc: [docs/guide/web-api-and-python-gateway.md](docs/guide/web-api-and-python-gateway.md).

## Layout

| Path | Purpose |
|------|---------|
| [src/MonitorControlSDK/](src/MonitorControlSDK/) | NuGet package `Sony.MonitorControl` |
| [src/MonitorControl.Web/](src/MonitorControl.Web/) | HTTP JSON API + Swagger + browser UI |
| [src/MonitorControl.Cli/](src/MonitorControl.Cli/) | `monitorctl` |
| [examples/python-service/](examples/python-service/) | Optional Python gateway to the HTTP API |
| [examples/arduino-knobs-brightness-contrast/](examples/arduino-knobs-brightness-contrast/) | ESP32/ESP8266: ADC pots → brightness/contrast via HTTP API |
| [examples/esp32-sdcp-vmc/](examples/esp32-sdcp-vmc/) | ESP32: same knobs over native TCP SDCP (no gateway PC) |
| [scripts/](scripts/) | `fetch-openapi.sh` / `generate-c-client.sh` for OpenAPI → C |
| [samples/](samples/) | Runnable examples (TCP broadcast REPL, SDAP discovery, **UDP VMC broadcast** sample) |
| [docs/](docs/) | **Authoritative** protocol and API documentation |
| [references/](references/) | Decompiled legacy Sony tooling (read-only); parity in [docs/reference/references-parity.md](docs/reference/references-parity.md) |
| [tests/](tests/) | Unit tests |
| [docs/plan/00-inventory.md](docs/plan/00-inventory.md) | Source file → concern map |

## Publish checklist

[docs/READY-TO-PUSH.md](docs/READY-TO-PUSH.md) — HTTPS remote and pre-push review.

## Legal

Protocol documentation is for interoperability. Distribution and field use may require your own legal review for trademark and IP.
