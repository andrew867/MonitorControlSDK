# .NET samples

Each project is a **minimal console app** demonstrating one concern. Build the solution (`dotnet build MonitorControl.sln`) then `dotnet run --project samples/<Name>` with the arguments below.

| Project | Demonstrates | Run |
|---------|----------------|-----|
| **Sample.Discovery** | SDAP listen window, print IP / product / serial | `dotnet run --project samples/Sample.Discovery` |
| **Sample.Vmc** | TCP SDCP `STATget` | `dotnet run --project samples/Sample.Vmc -- <host> <STATget-field>` |
| **Sample.Vms** | VMS product information exchange | `dotnet run --project samples/Sample.Vms -- <host>` |
| **Sample.Vma** | VMA control software version read | `dotnet run --project samples/Sample.Vma -- <host>` |
| **Sample.UdpVmcBroadcast** | One-shot UDP SDCP VMC (All monitors) | `dotnet run --project samples/Sample.UdpVmcBroadcast` — optional first arg: broadcast IP (e.g. subnet `192.168.1.255`) |
| **Sample.BroadcastControl** | Interactive REPL: TCP session + UDP broadcast helpers | `dotnet run --project samples/Sample.BroadcastControl -- <host>` — see [Sample.BroadcastControl/README.md](Sample.BroadcastControl/README.md) |

**Docs hub:** [docs/index.md](../docs/index.md) · **Handbook:** [docs/handbook.md](../docs/handbook.md)
