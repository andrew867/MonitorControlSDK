# MonitorControlSDK — Sony monitor SDAP / SDCP

.NET 8 client library (**Sony.MonitorControl**), operator CLI (**monitorctl**), and samples for SDAP discovery and SDCP control of compatible Sony professional monitors.

**Upstream (HTTPS):** [https://github.com/andrew867/MonitorControlSDK](https://github.com/andrew867/MonitorControlSDK)

**License:** [LICENSE](LICENSE) (MIT, Copyright 2026 Andrew Green).

## Documentation (start here)

Full tutorial + opcode reference + firmware guide: **[docs/index.md](docs/index.md)**  
**10-minute path:** [docs/quickstart.md](docs/quickstart.md)

## Quick commands

```bash
dotnet build Sony.MonitorControl.sln -c Release
dotnet test Sony.MonitorControl.sln -c Release
dotnet run --project src/MonitorControl.Cli -- discover
dotnet run --project src/MonitorControl.Cli -- vmc --host 192.168.0.10 MODEL
```

## Layout

| Path | Purpose |
|------|---------|
| [src/MonitorControlSDK/](src/MonitorControlSDK/) | NuGet package `Sony.MonitorControl` |
| [src/MonitorControl.Cli/](src/MonitorControl.Cli/) | `monitorctl` |
| [samples/](samples/) | Runnable examples (including broadcast REPL) |
| [docs/](docs/) | **Authoritative** protocol and API documentation |
| [tests/](tests/) | Unit tests |
| [docs/plan/00-inventory.md](docs/plan/00-inventory.md) | Source file → concern map |

## Publish checklist

[docs/READY-TO-PUSH.md](docs/READY-TO-PUSH.md) — HTTPS remote and pre-push review.

## Legal

Protocol documentation is for interoperability. Distribution and field use may require your own legal review for trademark and IP.
