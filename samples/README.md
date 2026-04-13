# .NET samples

Each project is a **minimal console app** demonstrating one concern. All of these projects are listed in [`MonitorControl.sln`](../MonitorControl.sln) and are built and tested in CI (`dotnet build` / `dotnet test` on the solution).

Build locally: `dotnet build MonitorControl.sln -c Release`, then `dotnet run --project samples/<ProjectDir>` with the arguments below.

| Project | Demonstrates | Run |
|---------|----------------|-----|
| **Sample.Discovery** | SDAP listen window (~5 s), print IP / product / serial | `dotnet run --project samples/Sample.Discovery` |
| **Sample.Vmc** | TCP SDCP `STATget` (one shot) | `dotnet run --project samples/Sample.Vmc -- <host> <STATget-field>` |
| **Sample.Vms** | VMS product information exchange | `dotnet run --project samples/Sample.Vms -- <host>` |
| **Sample.Vma** | VMA control software version read | `dotnet run --project samples/Sample.Vma -- <host>` |
| **Sample.UdpVmcBroadcast** | One-shot **UDP** SDCP VMC (All monitors scope in sample code) | `dotnet run --project samples/Sample.UdpVmcBroadcast` — optional first arg: broadcast IP (e.g. `192.168.1.255`) |
| **Sample.BroadcastControl** | Interactive REPL: **one long-lived TCP** session, repeated `get` / `set` (VMC only — **no** UDP in this sample) | `dotnet run --project samples/Sample.BroadcastControl -- <host>` — see [Sample.BroadcastControl/README.md](Sample.BroadcastControl/README.md) |

**UDP multi-monitor** from the CLI (not this sample): `monitorctl vmc-broadcast …` — see [docs/quickstart.md](../docs/quickstart.md).

**MCU on-wire** (Arduino, not .NET): [examples/esp32-sdcp-vmc](../examples/esp32-sdcp-vmc/), [examples/arduino-knobs-brightness-contrast](../examples/arduino-knobs-brightness-contrast/).

**Docs hub:** [docs/index.md](../docs/index.md) · **Handbook:** [docs/handbook.md](../docs/handbook.md) · **Diagrams:** [docs/diagrams/monitor-control-flows.md](../docs/diagrams/monitor-control-flows.md)
