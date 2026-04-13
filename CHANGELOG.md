# Changelog

All notable changes to this project are documented here. The format is inspired by [Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versioning follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html) via the `<Version>` property in `src/MonitorControlSDK/MonitorControlSDK.csproj` and matching `v*` git tags.

## [Unreleased]

### Documentation

- Rewrote [docs/diagrams/monitor-control-flows.md](docs/diagrams/monitor-control-flows.md): integration matrix, ESP32 **native TCP** sequence, Python gateway **SSE vs WebSocket**, dual physical UI paths, `Sample.BroadcastControl` REPL, corrected MCU topology (ESP32 on-wire does not go through HTTP).
- Expanded [docs/handbook.md](docs/handbook.md), [examples/README.md](examples/README.md), [samples/README.md](samples/README.md), [samples/Sample.BroadcastControl/README.md](samples/Sample.BroadcastControl/README.md), [docs/testing/broadcast-realtime-control-tests.md](docs/testing/broadcast-realtime-control-tests.md) (fixed test class names; CI note), [docs/plan/broadcast-realtime-control.md](docs/plan/broadcast-realtime-control.md), [examples/python-service/README.md](examples/python-service/README.md), [examples/arduino-knobs-brightness-contrast/README.md](examples/arduino-knobs-brightness-contrast/README.md), [docs/guide/web-api-and-python-gateway.md](docs/guide/web-api-and-python-gateway.md), [docs/quickstart.md](docs/quickstart.md), [docs/index.md](docs/index.md), [docs/spec/vmc-string-catalog.md](docs/spec/vmc-string-catalog.md), [README.md](README.md) ESP32 row, [docs/guide/network-and-debug.md](docs/guide/network-and-debug.md), [docs/guide/firmware-updates.md](docs/guide/firmware-updates.md), [docs/testing/strategy.md](docs/testing/strategy.md) (`nuget.config` path).

## [0.3.0] — 2026-04-13

### Documentation

- Added [docs/handbook.md](docs/handbook.md): single narrative for protocol stack, trust boundaries, SDK/CLI/HTTP surfaces, OpenAPI lifecycle, and QA pointers.
- Refreshed [docs/diagrams/monitor-control-flows.md](docs/diagrams/monitor-control-flows.md): CLI and `monitorctl`, full HTTP route inventory (REST + push), SDK layering, alignment with committed OpenAPI.
- Added [samples/README.md](samples/README.md) and [examples/README.md](examples/README.md) as catalogs for runnable .NET samples and optional gateways/MCU sketches.
- Clarified that in-repo synthesis is sufficient for SDCP monitor work; [docs/reference/external-sources.md](docs/reference/external-sources.md) is explicitly optional cross-check material.
- [docs/guide/openapi-codegen.md](docs/guide/openapi-codegen.md) and [docs/shipping/release-checklist.md](docs/shipping/release-checklist.md) now describe the committed OpenAPI artifact.

### OpenAPI

- **Committed** [openapi/monitorcontrol.openapi.json](openapi/monitorcontrol.openapi.json) (previously gitignored). Added [openapi/README.md](openapi/README.md).

[0.3.0]: https://github.com/andrew867/MonitorControlSDK/releases/tag/v0.3.0
