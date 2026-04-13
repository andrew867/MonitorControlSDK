# Sony monitor control (SDAP / SDCP)

This repository contains legacy reference applications (`MonitorNetwork`, Auto White Balance tools, firmware updater) and a new **.NET 8** library plus CLI for SDAP discovery and SDCP control.

## Quick start

```bash
dotnet build Sony.MonitorControl.sln -c Release
dotnet test tests/MonitorControlSDK.Tests/MonitorControlSDK.Tests.csproj -c Release
dotnet run --project src/MonitorControl.Cli -- discover
dotnet run --project src/MonitorControl.Cli -- vms-info --host 192.168.0.10
```

## Layout

| Path | Purpose |
|------|---------|
| [src/MonitorControlSDK/](src/MonitorControlSDK/) | NuGet package `Sony.MonitorControl` — protocol buffers, TCP/UDP transport, `VmsCommandEngine`, clients. |
| [src/MonitorControl.Cli/](src/MonitorControl.Cli/) | `monitorctl` operator CLI. |
| [samples/](samples/) | Minimal examples: SDAP, VMC, VMS, VMA. |
| [docs/spec/](docs/spec/) | Wire format and command catalogs. |
| [docs/plan/00-inventory.md](docs/plan/00-inventory.md) | Legacy source map and canonical baseline. |
| [MonitorNetwork/](MonitorNetwork/) | Original `net48` reference port (kept for diffing). |

## Documentation

See [docs/spec/sdcp-overview.md](docs/spec/sdcp-overview.md) and [docs/testing/strategy.md](docs/testing/strategy.md).

## Legal

Protocol behavior is derived from interoperability research in this repo. Distribution of a control SDK may require your own legal review for trademark and IP.
