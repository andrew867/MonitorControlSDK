# MonitorControlSDK — SDAP / SDCP monitor control

[![build-and-pack](https://github.com/andrew867/MonitorControlSDK/actions/workflows/build.yml/badge.svg?branch=master)](https://github.com/andrew867/MonitorControlSDK/actions/workflows/build.yml)

**MonitorControlSDK** is a **.NET 8** toolkit for discovering and controlling compatible **professional monitors** that advertise **SDAP** and accept **SDCP** on the documented ports. It implements **SDAP** (UDP discovery) and **SDCP** (TCP control), with optional **UDP SDCP** paths for VMC-style broadcast where supported.

The repository ships:

- **`MonitorControl.Sdk`** (NuGet) — class library: framing, transport, VMC / VMS / VMA-oriented clients.
- **`monitorctl`** — command-line tool for discovery, queries, and scripted control.
- **`MonitorControl.Web`** — optional HTTP JSON API, OpenAPI, and small browser UI for integration with other languages and frontends.
- **Samples and firmware-style examples** — runnable .NET samples plus Arduino / ESP32 sketches that use the HTTP API or raw SDCP.

**Repository:** [github.com/andrew867/MonitorControlSDK](https://github.com/andrew867/MonitorControlSDK)  
**License:** [MIT](LICENSE) (Copyright 2026 Andrew Green)

---

## Recent release (v0.3.0, 2026-04-13)

Patch-style documentation release **0.3.0** follows [Semantic Versioning](https://semver.org/). It adds an [**engineering handbook**](docs/handbook.md), expands Mermaid diagrams (CLI, full HTTP surface, SDK layering), indexes [`samples/README.md`](samples/README.md) and [`examples/README.md`](examples/README.md), and **commits** the OpenAPI 3 snapshot at [`openapi/monitorcontrol.openapi.json`](openapi/monitorcontrol.openapi.json) for offline codegen and review. No intentional wire-protocol or public API breaking changes from **0.2.0**. Earlier: [v0.2.0](https://github.com/andrew867/MonitorControlSDK/releases/tag/v0.2.0), [v0.1.2](https://github.com/andrew867/MonitorControlSDK/releases/tag/v0.1.2), [v0.1.1](https://github.com/andrew867/MonitorControlSDK/releases/tag/v0.1.1).

---

## Documentation

| Resource | Description |
|----------|-------------|
| **[docs/index.md](docs/index.md)** | Documentation hub: tutorials, opcode references, firmware notes |
| **[docs/handbook.md](docs/handbook.md)** | Engineering handbook: protocols, implementation map, surfaces, OpenAPI, safety |
| **[docs/quickstart.md](docs/quickstart.md)** | Short onboarding: build, discover, first commands |
| **[openapi/monitorcontrol.openapi.json](openapi/monitorcontrol.openapi.json)** | Committed OpenAPI 3 spec (regenerate with `bash scripts/fetch-openapi.sh`) |
| **[docs/reference/references-parity.md](docs/reference/references-parity.md)** | Map between `references/` snapshots and the shipped SDK |
| **[docs/ci-cd.md](docs/ci-cd.md)** | GitHub Actions and GitLab CI/CD setup |

---

## Build and run

```bash
dotnet build MonitorControl.sln -c Release
dotnet test MonitorControl.sln -c Release
dotnet run --project src/MonitorControl.Cli -- discover
dotnet run --project src/MonitorControl.Cli -- vmc --host 192.168.0.10 MODEL
dotnet run --project src/MonitorControl.Cli -- vmc-broadcast --scope all -- STATset BRIGHTNESS 512
```

---

## HTTP API and web UI

```bash
dotnet run --project src/MonitorControl.Web --urls http://127.0.0.1:5080
```

- UI: `http://127.0.0.1:5080/`
- OpenAPI: `http://127.0.0.1:5080/swagger`

Optional Python gateway: [examples/python-service/README.md](examples/python-service/README.md).  
Full guide: [docs/guide/web-api-and-python-gateway.md](docs/guide/web-api-and-python-gateway.md).

---

## Repository layout

| Path | Contents |
|------|----------|
| [src/MonitorControlSDK/](src/MonitorControlSDK/) | Library packaged as **`MonitorControl.Sdk`** on NuGet |
| [src/MonitorControl.Web/](src/MonitorControl.Web/) | HTTP JSON API, Swagger, browser UI |
| [src/MonitorControl.Cli/](src/MonitorControl.Cli/) | **`monitorctl`** CLI |
| [examples/python-service/](examples/python-service/) | Optional Python service in front of the HTTP API |
| [examples/arduino-knobs-brightness-contrast/](examples/arduino-knobs-brightness-contrast/) | ESP32 / ESP8266: analog inputs → brightness / contrast via HTTP |
| [examples/esp32-sdcp-vmc/](examples/esp32-sdcp-vmc/) | ESP32: same idea over native TCP SDCP |
| [scripts/](scripts/) | OpenAPI fetch / C client generation helpers; [openapi/](openapi/) holds the committed spec |
| [samples/](samples/) | Runnable .NET samples (including UDP VMC broadcast) |
| [docs/](docs/) | Protocol and product documentation maintained with the code |
| [references/](references/) | Read-only reference snapshots; see parity doc above |
| [tests/](tests/) | Unit tests |
| [docs/plan/00-inventory.md](docs/plan/00-inventory.md) | Source file → responsibility map |

---

## Releases, NuGet, and CI/CD

- **Changelog:** [CHANGELOG.md](CHANGELOG.md)
- **Versioning:** Git tags of the form `vMAJOR.MINOR.PATCH`; package version is set in [`src/MonitorControlSDK/MonitorControlSDK.csproj`](src/MonitorControlSDK/MonitorControlSDK.csproj).
- **NuGet:** [MonitorControl.Sdk](https://www.nuget.org/packages/MonitorControl.Sdk) (when published for a given tag).
- **Automation:** [docs/ci-cd.md](docs/ci-cd.md) describes GitHub Actions (build matrix, releases, GitHub Packages, optional NuGet.org) and GitLab CI for self-hosted runners.

---

## Contributing and release hygiene

[docs/READY-TO-PUSH.md](docs/READY-TO-PUSH.md) — remotes, HTTPS, and pre-push checks.

---

## Legal

Protocol documentation is provided for interoperability. Trademarks belong to their owners. Confirm behavior on your hardware and firmware before production or field deployment; obtain appropriate legal review where needed.
