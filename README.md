# MonitorControlSDK — Sony monitor SDAP / SDCP

[![build-and-pack](https://github.com/andrew867/MonitorControlSDK/actions/workflows/build.yml/badge.svg?branch=master)](https://github.com/andrew867/MonitorControlSDK/actions/workflows/build.yml)

Talk to your PVM like it’s 3023, not 2003: **.NET 8** library (**Sony.MonitorControl**), a **`monitorctl`** CLI that actually wants to be typed at, plus samples for **SDAP** discovery and **SDCP** control of compatible Sony professional monitors. Plug in, discover, nudge brightness, feel like a broadcast wizard.

**Upstream:** [github.com/andrew867/MonitorControlSDK](https://github.com/andrew867/MonitorControlSDK) · **License:** [MIT](LICENSE) (Copyright 2026 Andrew Green)

---

## What’s new in v0.1.1 (2026-04-13)

Semver is officially in the house: **0.1.1** is the current **NuGet** / assembly line. This patch zaps a sneaky CI-only compile issue (`byte[]` + `Reverse()` resolving to `Span`’s in-place `void` reverse). We now clone and `Array.Reverse` like civilized endian wranglers. Your GitHub Actions matrix should go green again on Windows, Linux, and macOS.

---

## Documentation (start here)

| Doc | Why open it |
|-----|----------------|
| **[docs/index.md](docs/index.md)** | Full tutorial + opcode reference + firmware guide |
| **[docs/quickstart.md](docs/quickstart.md)** | ~10-minute happy path |
| **[docs/reference/references-parity.md](docs/reference/references-parity.md)** | `references/` ↔ shipped SDK map |

---

## Quick commands

```bash
dotnet build Sony.MonitorControl.sln -c Release
dotnet test Sony.MonitorControl.sln -c Release
dotnet run --project src/MonitorControl.Cli -- discover
dotnet run --project src/MonitorControl.Cli -- vmc --host 192.168.0.10 MODEL
dotnet run --project src/MonitorControl.Cli -- vmc-broadcast --scope all -- STATset BRIGHTNESS 512
```

---

## HTTP API + web UI (for any frontend)

```bash
dotnet run --project src/MonitorControl.Web --urls http://127.0.0.1:5080
```

Open `http://127.0.0.1:5080/` (UI) and `http://127.0.0.1:5080/swagger` (OpenAPI). Optional Python proxy: [examples/python-service/README.md](examples/python-service/README.md). Full guide: [docs/guide/web-api-and-python-gateway.md](docs/guide/web-api-and-python-gateway.md).

---

## Layout

| Path | Purpose |
|------|---------|
| [src/MonitorControlSDK/](src/MonitorControlSDK/) | NuGet package **`Sony.MonitorControl`** |
| [src/MonitorControl.Web/](src/MonitorControl.Web/) | HTTP JSON API + Swagger + browser UI |
| [src/MonitorControl.Cli/](src/MonitorControl.Cli/) | **`monitorctl`** |
| [examples/python-service/](examples/python-service/) | Optional Python gateway to the HTTP API |
| [examples/arduino-knobs-brightness-contrast/](examples/arduino-knobs-brightness-contrast/) | ESP32/ESP8266: ADC pots → brightness/contrast via HTTP API |
| [examples/esp32-sdcp-vmc/](examples/esp32-sdcp-vmc/) | ESP32: same knobs over native TCP SDCP (no gateway PC) |
| [scripts/](scripts/) | `fetch-openapi.sh` / `generate-c-client.sh` for OpenAPI → C |
| [samples/](samples/) | Runnable examples (TCP broadcast REPL, SDAP discovery, **UDP VMC broadcast** sample) |
| [docs/](docs/) | **Authoritative** protocol and API documentation |
| [references/](references/) | Historical reference snapshots (read-only); parity in [docs/reference/references-parity.md](docs/reference/references-parity.md) |
| [tests/](tests/) | Unit tests |
| [docs/plan/00-inventory.md](docs/plan/00-inventory.md) | Source file → concern map |

---

## Releases & NuGet

- **Tags:** `v0.1.1`, `v0.1.0`, … — semver from here on out.
- **Package:** `Sony.MonitorControl` on NuGet (version tracks `<Version>` in the SDK csproj).
- **CI/CD:** [docs/ci-cd.md](docs/ci-cd.md) — GitHub Actions (public matrix + releases + GitHub Packages + optional NuGet.org) and GitLab CI for **self-hosted** Linux/Windows runners.

---

## Publish checklist

[docs/READY-TO-PUSH.md](docs/READY-TO-PUSH.md) — HTTPS remote and pre-push review.

---

## Legal

Protocol documentation is for interoperability. Distribution and field use may require your own legal review for trademark and IP.
