# Changelog

All notable changes to this project are documented here. The format is inspired by [Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versioning follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html) via the `<Version>` property in `src/MonitorControlSDK/MonitorControlSDK.csproj` and matching `v*` git tags.

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
